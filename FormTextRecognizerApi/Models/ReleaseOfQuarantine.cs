using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FormTextRecognizerApi.Models
{
    public record ReleaseOfQuarantine
    {
        public string FormDate { get; set; }
        public string FormTime { get; set; }
        public string FormCounty { get; set; }
        public string PremiseNameAndAddress { get; set; }
        public string ContactInformationFOrOwner { get; set; }
        public string AnimalDescription { get; set; }
        public string AnimalLocation { get; set; }
        public string ReleaseOfQuarantineDate { get; set; }
        public string QuarantinedPlacedOn { get; set; }
        public string ConditionDescription { get; set; }

    }
}
