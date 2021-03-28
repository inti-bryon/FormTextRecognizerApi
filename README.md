# FormTextRecognizerApi
## Form Recognizer Sample API

Sample .NET 5.0 API used to demo the **FormRecognition** Cognitive Service.

To run locally, you must update the lines of code: 

        private static readonly string endpoint = "[ENTER YOUR API ENDPOINT]";
        private static readonly string apiKey = "[ENTER YOUR API KEY]";
        
        
1. Use the *api/FormRecognizer* endpoint for general text recognition and a simple string return
 A. POST request body: {  "formURL": "*[ENTER THE SAS URL OF THE FORM TO ANALYZE]*" } 

2.Use the *api/DynamicCustomModel* endpoint to analyze a form using a custom created model; model is deleted after use.
 A. POST request body:  {  "formURL": "*[ENTER THE SAS URL OF THE FORM TO ANALYZE]*",  "locationURL": "*[ENTER THE SAS URL OF THE BLOB LOCATION WITH FORMS FOR MODEL CREATION]*"}

3. Use the *api/CustomModel* endpoint to analyze a form using a previously created custom model.
 A. POST request body:  {  "formURL": "*[ENTER THE SAS URL OF THE FORM TO ANALYZE]*",  "modelID": "*[ENTER THE GUID OF THE MODEL TO USE]*"}

  
   
