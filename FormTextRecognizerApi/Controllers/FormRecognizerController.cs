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
        private static readonly string endpoint = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";
        private static readonly string apiKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";
        private static readonly AzureKeyCredential credential = new AzureKeyCredential(apiKey);
        private static string returnString = string.Empty;

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
        [Route("api/CustomModel")]
        public ActionResult CustomForm([FromBody] FormCustom newForm)
        {
            returnString = string.Empty;

            var recognizerClient = AuthenticateClient();

            var analyzeForm = AnalyzeCustomForm(recognizerClient, newForm.locationURL, newForm.formURL);            
            Task.WaitAll(analyzeForm);

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

        private static async Task AnalyzeCustomForm(FormRecognizerClient recognizerClient, string trainingFileUrl, string formUrl)
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
        #endregion
    }
}