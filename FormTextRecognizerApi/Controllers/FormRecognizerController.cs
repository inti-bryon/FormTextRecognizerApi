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
        private static readonly string endpoint = "https://XXXXXXXXXXXXXXX.cognitiveservices.azure.com/";
        private static readonly string apiKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";
        private static readonly AzureKeyCredential credential = new AzureKeyCredential(apiKey);
        private static string formUrl = string.Empty;
        private static string returnString = string.Empty;
        private static ReleaseOfQuarantine roq;

        #endregion

        #region Public EndPoints

        [HttpPost]
        [Route("api/FormRecognizer")]
        public ActionResult FormRecognizer([FromBody] Form newForm)
        {
            formUrl = newForm.formURL;
            returnString = string.Empty;

            var recognizerClient = AuthenticateClient();
            var trainingClient = AuthenticateTrainingClient();

            var recognizeContent = RecognizeContent(recognizerClient);
            Task.WaitAll(recognizeContent);

            return Ok(returnString);
        }

        [HttpPost]
        [Route("api/ReceiptReader")]
        public ActionResult ReceiptReader([FromBody] Form newForm)
        {
            formUrl = newForm.formURL;
            returnString = string.Empty;

            var recognizerClient = AuthenticateClient();
            var trainingClient = AuthenticateTrainingClient();

            var analyzeReceipt = AnalyzeReceipt(recognizerClient);
            Task.WaitAll(analyzeReceipt);

            return Ok(returnString);
        }

        [HttpPost]
        [Route("api/ReleaseOfQuarantineForm")]
        public ActionResult ReleaseOfQuarantineForm([FromBody] Form newForm)
        {
            formUrl = newForm.formURL;
            returnString = string.Empty;

            var recognizerClient = AuthenticateClient();
            var trainingClient = AuthenticateTrainingClient();

            var analyzeForm = RecognizeReleaseOfQuarantine(recognizerClient);
            Task.WaitAll(analyzeForm);

            return Ok(roq);
        }

        [HttpPost]
        [Route("api/CustomModel")]
        public ActionResult CustomForm([FromBody] FormCustom newForm)
        {
            formUrl = newForm.formURL;
            returnString = string.Empty;

            var recognizerClient = AuthenticateClient();
            var trainingClient = AuthenticateTrainingClient();

            var analyzeForm = AnalyzeCustomForm(recognizerClient, newForm.modelID);
            Task.WaitAll(analyzeForm);

            return Ok(roq);
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

        private static async Task RecognizeContent(FormRecognizerClient recognizerClient)
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

        # region Receipt Reader
        private static async Task AnalyzeReceipt(FormRecognizerClient recognizerClient)
        {
            RecognizedFormCollection receipts = await recognizerClient.StartRecognizeReceiptsFromUri(new Uri(formUrl)).WaitForCompletionAsync();

            foreach (RecognizedForm receipt in receipts)
            {
                FormField merchantNameField;
                if (receipt.Fields.TryGetValue("COUNTY", out merchantNameField))
                {
                    if (merchantNameField.Value.ValueType == FieldValueType.String)
                    {
                        string merchantName = merchantNameField.Value.AsString();

                        returnString += $"COunty Name: '{merchantName}', with confidence {merchantNameField.Confidence}{Environment.NewLine}";
                    }
                }

                FormField transactionDateField;
                if (receipt.Fields.TryGetValue("TransactionDate", out transactionDateField))
                {
                    if (transactionDateField.Value.ValueType == FieldValueType.Date)
                    {
                        DateTime transactionDate = transactionDateField.Value.AsDate();

                        returnString += $"Transaction Date: '{transactionDate}', with confidence {transactionDateField.Confidence}{Environment.NewLine}";
                    }
                }
                FormField totalField;
                if (receipt.Fields.TryGetValue("TIME", out totalField))
                {
                    if (totalField.Value.ValueType == FieldValueType.String)
                    {
                        string aTime = totalField.Value.AsString();

                        returnString += $"TIme: '{aTime}', with confidence '{totalField.Confidence}'{Environment.NewLine}";
                    }
                }
                FormField toField;
                if (receipt.Fields.TryGetValue("To", out toField))
                {
                    if (toField.Value.ValueType == FieldValueType.String)
                    {
                        string total = toField.Value.AsString();

                        returnString += $"To:: '{total}', with confidence '{toField.Confidence}'{Environment.NewLine}";
                    }
                }
                FormField fromField;
                if (receipt.Fields.TryGetValue("From", out fromField))
                {
                    if (fromField.Value.ValueType == FieldValueType.String)
                    {
                        string total = fromField.Value.AsString();

                        returnString += $"From: '{total}', with confidence '{totalField.Confidence}'{Environment.NewLine}";
                    }
                }
            }

        }
        #endregion

        #region Custom Form

        private static async Task AnalyzeCustomForm2(FormRecognizerClient recognizerClient, string modelID)
        {
            RecognizeCustomFormsOperation operation = await recognizerClient.StartRecognizeCustomFormsFromUriAsync(modelID, new Uri(formUrl));
            Response<RecognizedFormCollection> operationResponse = await operation.WaitForCompletionAsync();
            RecognizedFormCollection forms = operationResponse.Value;

            foreach (RecognizedForm form in forms)
            {
                Console.WriteLine($"Form of type: {form.FormType}");
                //if (form.FormTypeConfidence.HasValue)
                //    Console.WriteLine($"Form type confidence: {form.FormTypeConfidence.Value}");
                //Console.WriteLine($"Form was analyzed with model with ID: {form.ModelId}");
                foreach (FormField field in form.Fields.Values)
                {
                    Console.WriteLine($"Field '{field.Name}': ");

                    if (field.LabelData != null)
                    {
                        Console.WriteLine($"  Label: '{field.LabelData.Text}'");
                    }

                    Console.WriteLine($"  Value: '{field.ValueData.Text}'");
                    Console.WriteLine($"  Confidence: '{field.Confidence}'");
                }
            }
        }
        private static async Task AnalyzeCustomForm(FormRecognizerClient recognizerClient, String modelId)
        {
            RecognizeCustomFormsOptions options = new RecognizeCustomFormsOptions();
            RecognizedFormCollection forms = await recognizerClient.StartRecognizeCustomFormsFromUri(modelId, new Uri(formUrl), options).WaitForCompletionAsync();

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
        #endregion

        #region Release Of Quarantine Form

        private static async Task RecognizeReleaseOfQuarantine(FormRecognizerClient recognizerClient)
        {
            FormPageCollection formPages = await recognizerClient
                .StartRecognizeContentFromUri(new Uri(formUrl))
                .WaitForCompletionAsync();
            roq = new ReleaseOfQuarantine();

            foreach (FormPage page in formPages)
            {
                for (int i = 0; i < page.Lines.Count; i++)
                {
                    FormLine line = page.Lines[i];

                    if (line.Text.Contains("DATE:"))
                    {
                        roq.FormDate = $"{page.Lines[i + 1].Text}";
                    }
                    if (line.Text.Contains("TIME"))
                    {
                        roq.FormTime = $"{page.Lines[i + 1].Text}";
                    }
                    if (line.Text.Contains("COUNTY"))
                    {
                        roq.FormCounty = $"{page.Lines[i + 1].Text}";
                    }
                    if (line.Text.Contains("PREMISE NAME AND ADDRESS"))
                    {
                        roq.PremiseNameAndAddress = $"{page.Lines[i + 2].Text} {page.Lines[i + 4].Text} {page.Lines[i + 6].Text}";
                        //until DESCRIPTION OF ANIMALS
                    }
                    if (line.Text.Contains("CONTACT INFORMATION FOR OWNER/REPRESENTATIVE"))
                    {
                        roq.ContactInformationFOrOwner = $"{page.Lines[i + 2].Text} {page.Lines[i + 4].Text} {page.Lines[i + 6].Text}";
                    }
                    if (line.Text.Contains("DESCRIPTION OF ANIMALS"))
                    {
                        if (page.Lines[i + 6].Text.Contains("RELEASE OF QUARANTINE"))
                        {
                            roq.AnimalDescription = $"{page.Lines[i + 2].Text}";
                        }
                        else if (page.Lines[i + 7].Text.Contains("RELEASE OF QUARANTINE"))
                        {
                            roq.AnimalDescription = $"{page.Lines[i + 2].Text} {page.Lines[i + 4].Text}";
                        }
                        else if (page.Lines[i + 9].Text.Contains("RELEASE OF QUARANTINE"))
                        {
                            roq.AnimalDescription = $"{page.Lines[i + 2].Text} {page.Lines[i + 4].Text} {page.Lines[i + 6].Text}";
                        }
                    }
                    if (line.Text.Contains("PHYSICAL LOCATION OF ANIMALS"))
                    {
                        if (page.Lines[i + 5].Text.Contains("RELEASE OF QUARANTINE"))
                        {
                            roq.AnimalLocation = $"{page.Lines[i + 2].Text} {page.Lines[i + 3].Text} {page.Lines[i + 4].Text}";
                        }
                        else if (page.Lines[i + 6].Text.Contains("RELEASE OF QUARANTINE"))
                        {
                            roq.AnimalLocation = $"{page.Lines[i + 2].Text} {page.Lines[i + 4].Text} {page.Lines[i + 5].Text}";
                        }
                    }
                    if (line.Text.Contains("RELEASE OF QUARANTINE") && i > 10)
                    {
                        roq.ReleaseOfQuarantineDate = $"{page.Lines[i + 1].Text}";
                    }
                    if (line.Text.Contains("Quarantine placed"))
                    {
                        roq.QuarantinedPlacedOn = $"{page.Lines[i + 1].Text}";
                    }
                    if (line.Text.Contains("Check if any conditions for Release and Describe Conditions"))
                    {
                        int startIndex = i + 1;
                        while (!page.Lines[startIndex].Text.Contains("ACKNOWLEDGEMENT AND SIGNATURE"))
                        {
                            roq.ConditionDescription += $"{page.Lines[startIndex].Text}";
                            startIndex++;
                        }
                    }
                }
            }
        }

        #endregion

    }
}
