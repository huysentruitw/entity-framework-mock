using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkMock.NSubstitute.Tests.Models
{
    [Table("LoggingRepository")]
    public class IntKeyModel
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LoggingRepositoryId { get; set; }

        public string Url { get; set; }
    }
}
