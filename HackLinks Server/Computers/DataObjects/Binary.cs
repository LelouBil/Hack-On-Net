using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HackLinks_Server.Computers.DataObjects {
	public class Binary {
		[Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Required]
		public int id { get; set; }
		
		[Required]
		public int checksum { get; set; }

		[Required] [StringLength(64)] 
		public string type { get; set; }

		public static IEnumerable<Binary> Defaults { get; }
	}
}