using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HackLinks_Server.Computers.DataObjects {
	[Table("accounts")]
	public class ServerAccount{
		
		[Required] [Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int id { get; set; }
		
		[Index] [Required] [StringLength(64)]
		public string username { get; set; }
		
		[StringLength(64)]
		public string password { get; set; }
		
		[Index] [StringLength(64)]
		public string mailaddress  { get; set; }
		
		[Required]
		public string netmap { get; set; }
		
		public string content { get; set; }
		
		public int homeComputer { get; set; }

		[Required]
		public string permissions { get; set; }
		
		public bool banned { get; set; }
		
		[Required]
		public bool permBanned { get; set; }

		public static IEnumerable<ServerAccount> Defaults { get; } = new List<ServerAccount>() {
			new ServerAccount() {
				id = 1,
				username = "test",
				password = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
				mailaddress = "test@hnmp.net",
				netmap = "",
				homeComputer = 1,
				permissions = "admin",
				banned = false,
				permBanned = false
			}
		};
	}
}