using System;
using System.Collections.Generic;

namespace GBA.Domain.Entities.PolishUserDetails;

public sealed class UserDetails : EntityBase {
    public UserDetails() {
        Users = new HashSet<User>();
    }

    public string FirstName { get; set; }

    public string MiddleName { get; set; }

    public string LastName { get; set; }

    public string FathersName { get; set; }

    public string MothersName { get; set; }

    public DateTime DateOfBirth { get; set; }

    public string Address { get; set; }

    public string Accommodation { get; set; }

    public string Registration { get; set; }

    public string Education { get; set; }

    public string Profession { get; set; }

    public double WorkExperience { get; set; }

    public string FamilyStatus { get; set; }

    public int NumberOfDependents { get; set; }

    public bool IsBigFamily { get; set; }

    public string SocialSecurityNumber { get; set; }

    public string TIN { get; set; }

    public string VATIN { get; set; }

    public string PassportNumber { get; set; }

    public DateTime DocumentsExpirationDate { get; set; }

    public DateTime MedicalCertificateToDate { get; set; }

    public string AdditionalSchools { get; set; }

    public string VocationalCourses { get; set; }

    public string BasicHealtAndSagetyEducation { get; set; }

    public string SpecializedTraining { get; set; }

    public double WorkHeight { get; set; }

    public bool HasPermissionToOperateCarts { get; set; }

    public string Caveats { get; set; }

    public long ResidenceCardId { get; set; }

    public long WorkingContractId { get; set; }

    public long WorkPermitId { get; set; }

    public WorkingContract WorkingContract { get; set; }

    public WorkPermit WorkPermit { get; set; }

    public ResidenceCard ResidenceCard { get; set; }

    public ICollection<User> Users { get; set; }
}