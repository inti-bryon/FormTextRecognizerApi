using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer;
using Azure.AI.FormRecognizer.Models;
using Azure.AI.FormRecognizer.Training;
using System.IO;
using FormTextRecognizerApi.Models;

namespace FormTextRecognizerApi.Controllers
{
    [ApiController]
    public class FormRecognizerController : ControllerBase
    {

        #region Static Variables 
        private static readonly string endpoint = "https://ibsample01.cognitiveservices.azure.com/";
        private static readonly string apiKey = "4fac4df99c1f4468895556b9e6811b82";
        private static readonly AzureKeyCredential credential = new AzureKeyCredential(apiKey);
        private static string returnString = string.Empty;
        private static List<FormResponseObj> returnObjects = new List<FormResponseObj>();

        #endregion

        #region Public EndPoints

        [HttpPost]
        [Route("api/FormRecognizer")]
        public ActionResult FormRecognizer([FromBody] Form newForm)
        {
            returnString = string.Empty;

            var recognizerClient = AuthenticateClient();

            var recognizeContent = RecognizeContent(recognizerClient, newForm.formURL);
            Task.WaitAll(recognizeContent);

            return Ok(returnString);
        }
        [HttpPost]
        [Route("api/DynamicCustomModel")]
        public ActionResult DynamicCustomForm([FromBody] FormCustom newForm)
        {
            returnString = string.Empty;

            var recognizerClient = AuthenticateClient();

            var analyzeForm = AnalyzeDynamicCustomForm(recognizerClient, newForm.locationURL, newForm.formURL);
            Task.WaitAll(analyzeForm);

            return Ok(returnString);
        }

        [HttpPost]
        [Route("api/CustomModel")]
        public ActionResult CustomForm([FromBody] FormModel newForm)
        {
            returnString = string.Empty;

            var recognizerClient = AuthenticateClient();

            var analyzeForm = AnalyzeCustomForm(recognizerClient, newForm.modelID, newForm.formURL);            
            Task.WaitAll(analyzeForm);

            return Ok(returnString);
        }

        [HttpPost]
        [Route("api/CustomModelReturnObj")]
        public ActionResult CustomFormReturnObj([FromBody] FormModel newForm)
        {
            returnString = string.Empty;


            var recognizerClient = AuthenticateClient();

            var analyzeForm = AnalyzeCustomFormReturnObj(recognizerClient, newForm.modelID, newForm.formURL);
            Task.WaitAll(analyzeForm);

            foreach(FormResponseObj o in returnObjects)
            {
                returnString = $"Pet Name: {o.PetsName}, Owner Name: {o.Owner}, Type of Animal: {o.Species}, Description: {o.Breed}, Color: {o.Color}";
            }

            return Ok(returnString);
        }

        //[HttpPost]
        //[Route("api/RemoveModel")]
        //public ActionResult RemoveModel([FromBody] FormDelete newForm)
        //{

        //    var recognizerClient = AuthenticateClient();
        //    var deleteModel = DeleteModel(recognizerClient, newForm.modelId);
        //    Task.WaitAll(deleteModel);

        //    return Ok($"Model ID#: {newForm.modelId} has been removed.");
        //}

        #endregion

        #region Delete Form
        private static async Task DeleteModel(FormRecognizerClient recognizerClient, string modelID)
        {
            FormTrainingClient trainingClient = new FormTrainingClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            await trainingClient.DeleteModelAsync(modelID);
        }

        #endregion

        #region Form Recognizer
        static private FormRecognizerClient AuthenticateClient()
        {
            var credential = new AzureKeyCredential(apiKey);
            var client = new FormRecognizerClient(new Uri(endpoint), credential);
            return client;
        }

        static private FormTrainingClient AuthenticateTrainingClient()
        {
            var credential = new AzureKeyCredential(apiKey);
            var client = new FormTrainingClient(new Uri(endpoint), credential);
            return client;
        }

        private static async Task RecognizeContent(FormRecognizerClient recognizerClient, string formUrl)
        {
            FormPageCollection formPages = await recognizerClient
                .StartRecognizeContentFromUri(new Uri(formUrl))
                .WaitForCompletionAsync();
            foreach (FormPage page in formPages)
            {
                //lines
                for (int i = 0; i < page.Lines.Count; i++)
                {
                    FormLine line = page.Lines[i];

                    //returnString += $"{line.Text}{Environment.NewLine}";
                    returnString += $"    Line {i} has {line.Words.Count} word{(line.Words.Count > 1 ? "s" : "")}, and text: '{line.Text}'.{Environment.NewLine}";
                }
                //tables
                for (int i = 0; i < page.Tables.Count; i++)
                {
                    FormTable table = page.Tables[i];
                    foreach (FormTableCell cell in table.Cells)
                    {
                        //returnString += $"{cell.Text} ";
                        returnString += $"    Cell ({cell.RowIndex}, {cell.ColumnIndex}) contains text: '{cell.Text}'.{Environment.NewLine}";
                    }
                }
            }
        }
        #endregion

        #region Custom Form

        private static async Task AnalyzeDynamicCustomForm(FormRecognizerClient recognizerClient, string trainingFileUrl, string formUrl)
        {
            RecognizeCustomFormsOptions options = new RecognizeCustomFormsOptions();

            //train model
            FormTrainingClient trainingClient = new FormTrainingClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            CustomFormModel model = await trainingClient.StartTrainingAsync(new Uri(trainingFileUrl), useTrainingLabels: false, 
                $"VIS-Dynamic-Model-{DateTime.Now.ToShortDateString()}-{DateTime.Now.ToLongTimeString()}").WaitForCompletionAsync();

            //string modelId = inModelID;
            string modelId = model.ModelId;

            //recognize form
            RecognizeCustomFormsOperation operation = await recognizerClient.StartRecognizeCustomFormsFromUriAsync(modelId, new Uri(formUrl));
            Response<RecognizedFormCollection> operationResponse = await operation.WaitForCompletionAsync();
            RecognizedFormCollection forms = operationResponse.Value;

            foreach (RecognizedForm form in forms)
            {
                returnString += $"Form of type: {form.FormType}{Environment.NewLine}";
                foreach (FormField field in form.Fields.Values)
                {
                    returnString += $"Field '{field.Name}: ";

                    if (field.LabelData != null)
                    {
                        returnString += $"    Label: '{field.LabelData.Text}";
                    }

                    returnString += $"    Value: '{field.ValueData.Text}";
                    returnString += $"    Confidence: '{field.Confidence}{Environment.NewLine}";
                }
                returnString += $"Table data:{Environment.NewLine}";
                foreach (FormPage page in form.Pages)
                {
                    for (int i = 0; i < page.Tables.Count; i++)
                    {
                        FormTable table = page.Tables[i];
                        //Console.WriteLine($"Table {i} has {table.RowCount} rows and {table.ColumnCount} columns.");
                        foreach (FormTableCell cell in table.Cells)
                        {
                            returnString += $"    Cell ({cell.RowIndex}, {cell.ColumnIndex}) contains {(cell.IsHeader ? "header" : "text")}: '{cell.Text}'{Environment.NewLine}";
                        }
                    }
                }
            }
            // Delete the model on completion to clean environment.
            await trainingClient.DeleteModelAsync(model.ModelId);

        }

        private static async Task AnalyzeCustomForm(FormRecognizerClient recognizerClient, string modelID, string formUrl)
        {
            RecognizeCustomFormsOptions options = new RecognizeCustomFormsOptions();

            //recognize form
            RecognizeCustomFormsOperation operation = await recognizerClient.StartRecognizeCustomFormsFromUriAsync(modelID, new Uri(formUrl));
            Response<RecognizedFormCollection> operationResponse = await operation.WaitForCompletionAsync();
            RecognizedFormCollection forms = operationResponse.Value;

            foreach (RecognizedForm form in forms)
            {
                returnString += $"Form of type: {form.FormType}{Environment.NewLine}";
                foreach (FormField field in form.Fields.Values)
                {
                    returnString += $"Field '{field.Name}: ";

                    if (field.LabelData != null)
                    {
                        returnString += $"    Label: '{field.LabelData.Text}";
                    }

                    returnString += $"    Value: '{field.ValueData.Text}";
                    returnString += $"    Confidence: '{field.Confidence}{Environment.NewLine}";
                }
                returnString += $"Table data:{Environment.NewLine}";
                foreach (FormPage page in form.Pages)
                {
                    for (int i = 0; i < page.Tables.Count; i++)
                    {
                        FormTable table = page.Tables[i];
                        //Console.WriteLine($"Table {i} has {table.RowCount} rows and {table.ColumnCount} columns.");
                        foreach (FormTableCell cell in table.Cells)
                        {
                            returnString += $"    Cell ({cell.RowIndex}, {cell.ColumnIndex}) contains {(cell.IsHeader ? "header" : "text")}: '{cell.Text}'{Environment.NewLine}";
                        }
                    }
                }
            }
        }

        private static async Task AnalyzeCustomFormReturnObj(FormRecognizerClient recognizerClient, string modelID, string formUrl)
        {
            RecognizeCustomFormsOptions options = new RecognizeCustomFormsOptions();

            //recognize form
            RecognizeCustomFormsOperation operation = await recognizerClient.StartRecognizeCustomFormsFromUriAsync(modelID, new Uri(formUrl));
            Response<RecognizedFormCollection> operationResponse = await operation.WaitForCompletionAsync();
            RecognizedFormCollection forms = operationResponse.Value;

            foreach (RecognizedForm form in forms)
            {
                FormResponseObj rObj = new FormResponseObj();

                foreach (FormField field in form.Fields.Values)
                {

                    switch(field.Name)
                    {
                        case "Birthdate":
                            {
                                rObj.Birthdate = field.ValueData.Text;
                            }
                            break;
                        case "Breed":
                            {
                                rObj.Breed = field.ValueData.Text;
                            }
                            break;
                        case "Color":
                            {
                                rObj.Color = field.ValueData.Text;
                            }
                            break;
                        case "DateOfVaccination":
                            {
                                rObj.DateOfVaccination = field.ValueData.Text;
                            }
                            break;
                        case "DueDate":
                            {
                                rObj.DueDate = field.ValueData.Text;
                            }
                            break;
                        case "Duration":
                            {
                                rObj.Duration = field.ValueData.Text;
                            }
                            break;
                        case "LotExpiration":
                            {
                                rObj.LotExpiration = field.ValueData.Text;
                            }
                            break;
                        case "Manufacturer":
                            {
                                rObj.Manufacturer = field.ValueData.Text;
                            }
                            break;
                        case "MicrochipNumber":
                            {
                                rObj.MicrochipNumber = field.ValueData.Text;
                            }
                            break;
                        case "Owner":
                            {
                                rObj.Owner = field.ValueData.Text;
                            }
                            break;
                        case "OwnerAddress":
                            {
                                rObj.OwnerAddress = field.ValueData.Text;
                            }
                            break;
                        case "OwnerPhone":
                            {
                                rObj.OwnerPhone = field.ValueData.Text;
                            }
                            break;
                        case "PetsName":
                            {
                                rObj.PetsName = field.ValueData.Text;
                            }
                            break;
                        case "SerialNumber":
                            {
                                rObj.SerialNumber = field.ValueData.Text;
                            }
                            break;
                        case "Sex":
                            {
                                rObj.Sex = field.ValueData.Text;
                            }
                            break;
                        case "Species":
                            {
                                rObj.Species = field.ValueData.Text;
                            }
                            break;
                        case "TagNumber":
                            {
                                rObj.TagNumber = field.ValueData.Text;
                            }
                            break;
                        case "VaccinationLocation":
                            {
                                rObj.VaccinationLocation = field.ValueData.Text;
                            }
                            break;
                        case "Weight":
                            {
                                rObj.Weight = field.ValueData.Text;
                            }
                            break;
                    }
                }

                returnObjects.Add(rObj);
            }
        }

        #endregion
    }
}