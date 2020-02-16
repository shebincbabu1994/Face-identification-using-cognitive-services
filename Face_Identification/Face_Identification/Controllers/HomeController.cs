using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System.Text;

namespace FaceDetectionProject.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {

            return View();

        }

        public IActionResult TrainPhoto()
        {
            return View();
        }

        public IActionResult RecognizeImage()
        {
            return View();
        }


        public static IFaceClient Authenticate(string endpoint, string key)
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
        }


        [HttpPost]
        public IActionResult UploadImage(AddPerson data)
        {
            IFaceClient faceClient = Authenticate("https://centralindia.api.cognitive.microsoft.com/", "YOUR_KEY");
            string personGroupId = "myfriends1";
            faceClient.PersonGroup.CreateAsync(personGroupId, "My friends").GetAwaiter().GetResult();  // FIrst time use only. After create group please remove it.
            var friend1 = faceClient.PersonGroupPerson.CreateAsync(
              personGroupId,
              data.Name
            ).GetAwaiter().GetResult();

            foreach (var image in data.files)
            {
                using (var ms = new MemoryStream())
                {
                    image.CopyTo(ms);
                    var fileBytes = ms.ToArray();
                    MemoryStream stream = new MemoryStream(fileBytes);
                    faceClient.PersonGroupPerson.AddFaceFromStreamAsync(personGroupId, friend1.PersonId, stream).GetAwaiter().GetResult();
                }
            }

            faceClient.PersonGroup.TrainAsync(personGroupId).GetAwaiter().GetResult();
            TrainingStatus trainingStatus = null;
            while (true)
            {
                trainingStatus = faceClient.PersonGroup.GetTrainingStatusAsync(personGroupId).GetAwaiter().GetResult();

                if (trainingStatus.Status != TrainingStatusType.Running)
                {
                    break;
                }

            }

            return Content("Student Add SuccessFully");
        }


        [HttpPost]
        public IActionResult Recognize(IFormFile file)
        {
            var namelist = "";

            IFaceClient faceClient = Authenticate("https://centralindia.api.cognitive.microsoft.com/", "YOUR_KEY");
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                var fileBytes = ms.ToArray();
                MemoryStream stream = new MemoryStream(fileBytes);
                var faces = faceClient.Face.DetectWithStreamAsync(stream).GetAwaiter().GetResult();

                var faceIds = faces.Select(face => face.FaceId.Value).ToArray();
                string personGroupId = "myfriends1";
                var results = faceClient.Face.IdentifyAsync(faceIds, personGroupId).GetAwaiter().GetResult();
                foreach (var identifyResult in results)
                {
                    if (identifyResult.Candidates.Count == 0)
                    {
                        Console.WriteLine("No one identified");
                    }
                    else
                    {
                        var candidateId = identifyResult.Candidates[0].PersonId;
                        var person = faceClient.PersonGroupPerson.GetAsync(personGroupId, candidateId).GetAwaiter().GetResult();
                        namelist = namelist + person.Name + ",";
                    }
                }
            }

            return Content(namelist);
        }

    }
    public class AddPerson
    {
        public List<IFormFile> files { get; set; }
        public string Name { get; set; }
    }

}

