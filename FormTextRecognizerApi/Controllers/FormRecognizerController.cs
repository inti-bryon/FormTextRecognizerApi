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
        private static string formUrl = string.Empty;
        private static string returnString = string.Empty;

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
        [Route("api/CustomForm")]
        public ActionResult CustomForm([FromBody] FormCustom newForm)
        {
            formUrl = newForm.formURL;
            returnString = string.Empty;

            var recognizerClient = AuthenticateClient();
            var trainingClient = AuthenticateTrainingClient();

            var analyzeForm = AnalyzePdfForm(recognizerClient,newForm.modelID);
            Task.WaitAll(analyzeForm);

            return Ok(returnString);
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

                    returnString += $"{line.Text}{Environment.NewLine}";
                }
                //tables
                for (int i = 0; i < page.Tables.Count; i++)
                {
                    FormTable table = page.Tables[i];
                    foreach (FormTableCell cell in table.Cells)
                    {
                        returnString += $"{cell.Text} ";
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
                if (receipt.Fields.TryGetValue("MerchantName", out merchantNameField))
                {
                    if (merchantNameField.Value.ValueType == FieldValueType.String)
                    {
                        string merchantName = merchantNameField.Value.AsString();

                        returnString += $"Merchant Name: '{merchantName}', with confidence {merchantNameField.Confidence}{Environment.NewLine}";
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
                if (receipt.Fields.TryGetValue("Total", out totalField))
                {
                    if (totalField.Value.ValueType == FieldValueType.Float)
                    {
                        float total = totalField.Value.AsFloat();

                        returnString += $"Total: '{total}', with confidence '{totalField.Confidence}'{Environment.NewLine}";
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
        private static async Task AnalyzePdfForm(FormRecognizerClient recognizerClient, String modelId) 
        {
            RecognizedFormCollection forms = await recognizerClient.StartRecognizeCustomFormsFromUri(modelId, new Uri(formUrl)).WaitForCompletionAsync();

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
    }
}
