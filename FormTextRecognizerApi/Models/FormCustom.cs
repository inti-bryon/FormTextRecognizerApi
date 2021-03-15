using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FormTextRecognizerApi.Models
{
    public record FormCustom
    {
        public string formURL { get; set; }
        public string modelID { get; set; }
    }
}
