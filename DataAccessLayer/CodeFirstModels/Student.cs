using System;
using System.Collections.Generic;
using System.Text;
using EFSupport;
using Newtonsoft.Json;

namespace DataAccessLayer.CodeFirstModels
{
    public class Student: IIntIdRepositoryBase
    {
        public int Id { get; set; }
        public string LastName { get; set; }
        public string FirstMidName { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public string FavoriteCourse { get; set; }

        [JsonIgnore]
        public virtual ICollection<Enrollment> Enrollments { get; set; }
    }
}

