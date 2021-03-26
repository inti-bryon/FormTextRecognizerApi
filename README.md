# FormTextRecognizerApi
## Form Recognizer Sample API

Sample .NET 5.0 API used to demo the **FormRecognition** Cognitive Service.

To run locally, you must update the lines of code: 

        private static readonly string endpoint = "[ENTER YOUR API ENDPOINT]";
        private static readonly string apiKey = "[ENTER YOUR API KEY]";
        
        
1. use the *api/FormRecognizer* endpoint for general text recognition and a simple string return
  a. POST request body: {  "formURL": "*[ENTER THE URL OF THE FORM TO ANALYZE]*" } 
2. use the *api/CustomModel* endpoint to analyze a form using a custom created model; model is deleted after use.
  a. POST request body:  {  "formURL": "*[ENTER THE URL OF THE FORM TO ANALYZE]*",  "locationURL": "*[ENTER THE URL OF THE BLOB LOCATION WITH FORMS FOR MODEL CREATION]*"}
3. use the *api/RemoveModel* endpoint to remove any non-used models 
  a. POST request body:  {  "modelId": "*[ENTER THE ID OF THE MODEL TO DELETE]*"}
  
  
