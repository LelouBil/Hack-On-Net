using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace HackLinks_Server.Computers.DataObjects {
	[Table("accounts")]
	public class ServerAccount{
		
		
		[Required] [Key] [StringLength(64)]
		public string username { get; set; }
		
		[StringLength(64)]
		public string password { get; set; }
		
		[StringLength(64)]
		public string mailaddress  { get; set; }
		
		[Required]
		public List<NetMapNode> netmap { get; set; }

		public class NetMapNode {
			[Required] [Key]
			public int id { get; set; }
			public NetMapNode(string ip, string pos) {
				this.ip = ip;
				this.pos = pos;
			}

			public string ip { get; set; }

			public NetMapNode() {
				
			}

			public static String maptoList(List<NetMapNode> list) {
				return string.Join(",", list.Select(x => x.ip + ":" + x.pos).ToArray());
			}
			
			public static List<NetMapNode> ListToMap(String s ) {
				return s.Split(',').Select(a => new NetMapNode(a.Split(':')[0], a.Split(':')[1])).ToList();
			}

			[Required]
			public string pos { get; set; } //todo replace by type
		}

		public string content { get; set; }
		
		public virtual Node homeComputer { get; set; }

		
		public List<HackLinks_Server.Permissions> permissions { get; set; }
		
        [Required]
		public int banned { get; set; }

		public static List<ServerAccount> Defaults { get; set; } = new List<ServerAccount>() {
			new ServerAccount() {
				username = "test",
				password = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
				mailaddress = "test@hnmp.net",
				netmap = new List<NetMapNode>(),
				homeComputer = Server.Instance.DatabaseLink.Computers.First(), //todo
				permissions = new List<HackLinks_Server.Permissions>(){HackLinks_Server.Permissions.Admin},
				banned = 0
			}
		};

		public List<Node> Nodes => Server.Instance.DatabaseLink.Computers.Where(s => s.owner.Equals(this)).ToList();
		public string StringMap => StringNMap();

		private string StringNMap() {
			return NetMapNode.maptoList(netmap);
		}

		public void SetUserBanStatus(bool ban, int expiry) {
			if (ban) {
				this.banned = expiry;
			}
			else {
				this.banned = 0;
			}

			Server.Instance.DatabaseLink.SaveChanges();
			GameClient client = Server.Instance.clients.Find(c => c.account.Equals(this));
			if(client == null) return;
			try {
				client.Send(HackLinksCommon.NetUtil.PacketType.DSCON, "You have been banned from the server. Reconnect to see ban expiry");
				client.netDisconnect();
			}
			catch (Exception e){}
		}

		public bool IsBanned(out int banExpiry) {
			try
			{
				if (banned == -1)
				{
					banExpiry = -1;
					return true;
				}
			}
			catch (Exception) {
				// ignored
			}

			try
			{
				if (this.banned > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
				{
					banExpiry = this.banned;
					return true;
				}
				if (this.banned <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
					SetUserBanStatus(false, 0);
			}
			catch (Exception) {
				// ignored
			}

			banExpiry = 0;
			return false;
		}
	}
}