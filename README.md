# FormTextRecognizerApi
## Form Recognizer Sample API

Sample .NET 5.0 API used to demo the **FormRecognition** Cognitive Service.

To run locally, you must update the lines of code: 

        private static readonly string endpoint = "[ENTER YOUR API ENDPOINT]";
        private static readonly string apiKey = "[ENTER YOUR API KEY]";
        
        
1. Use the *api/FormRecognizer* endpoint for general text recognition and a simple string return
2. POST request body: {  "formURL": "*[ENTER THE URL OF THE FORM TO ANALYZE]*" } 
3. Use the *api/CustomModel* endpoint to analyze a form using a custom created model; model is deleted after use.
4. POST request body:  {  "formURL": "*[ENTER THE URL OF THE FORM TO ANALYZE]*",  "locationURL": "*[ENTER THE URL OF THE BLOB LOCATION WITH FORMS FOR MODEL CREATION]*"}

  
   
