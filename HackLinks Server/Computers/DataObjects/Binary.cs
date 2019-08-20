using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using HackLinks_Server.Computers.Processes;

namespace HackLinks_Server.Computers.DataObjects {
	public class Binary {
		private Binary(string type1Name) {
			this.type = type1Name;
			this.checksum = type1Name.ToLower().GetHashCode();
		}

		public Binary() {
			
		}

		[Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Required]
		public int id { get; set; }
		
		[Required]
		public int checksum { get; set; }

		[Required] [StringLength(64)] 
		public string type { get; set; }

		public static IEnumerable<Binary> getBinaries() {
			List<Type> t = Getprocesses();
			foreach (var type1 in t) {
				yield return new Binary(type1.Name);
			}
		}
		
		public static List<Type> Getprocesses(){
			List<Type> objects = new List<Type>();
			foreach (Type type in 
				Assembly.GetAssembly(typeof(CommandProcess)).GetTypes()
					.Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(CommandProcess))))
			{
				objects.Add(type);
			}
			return objects;
		}
	}
}