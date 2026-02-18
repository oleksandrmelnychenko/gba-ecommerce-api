using System;
using System.Collections.Generic;

namespace GBA.Domain.Entities.PolishUserDetails;

public sealed class WorkingContract : EntityBase {
    public WorkingContract() {
        UsersDetails = new HashSet<UserDetails>();
    }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public string PlaceOfWork { get; set; }

    public string CurrentWorkplace { get; set; }

    public string KindOfWork { get; set; }

    public string Delegation { get; set; }

    public string Premium { get; set; }

    public string WorkTimeSize { get; set; }

    public string VacationDays { get; set; }

    public string NightWork { get; set; }

    public string StudyLeave { get; set; }

    public ICollection<UserDetails> UsersDetails { get; set; }
}