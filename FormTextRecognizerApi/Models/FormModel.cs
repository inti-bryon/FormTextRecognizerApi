using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FormTextRecognizerApi.Models
{
    public record FormModel
    {
        public string formURL { get; set; }
        public string modelID { get; set; }
    }
}
