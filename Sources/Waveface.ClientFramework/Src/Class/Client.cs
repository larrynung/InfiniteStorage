﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Waveface.Model;
using Microsoft.Win32;

namespace Waveface.ClientFramework
{
	public class Client
	{
		#region Static Var
		private static Client _default;
		#endregion


		#region Var
		private static string _labelID = Guid.Empty.ToString();
		private ObservableCollection<IContentEntity> _favorites;
		private ReadOnlyObservableCollection<IContentEntity> _readonlyFavorites;
		#endregion


		#region Public Static Property
		public static Client Default
		{
			get
			{
				return _default ?? (_default = new Client());
			}
		}
		#endregion


		#region Private Property
		public static string StarredLabelId
		{
			get
			{
				return _labelID;
			}
		}

		private ObservableCollection<IContentEntity> m_Favorites
		{
			get
			{
				if (_favorites == null)
				{
					_favorites = new ObservableCollection<IContentEntity>(GetFavorites());
				}
				return _favorites;
			}
		}
		#endregion


		#region Public Property
		public IEnumerable<IService> Services
		{
			get
			{
				return BunnyServiceSupplier.Default.Services;
			}
		}

		public ReadOnlyObservableCollection<IContentEntity> Favorites
		{
			get
			{
				return _readonlyFavorites ?? (_readonlyFavorites = new ReadOnlyObservableCollection<IContentEntity>(m_Favorites));
			}
		}
		#endregion


		public Client()
		{
			foreach (var service in BunnyServiceSupplier.Default.Services)
			{
				service.ContentPropertyChanged += service_ContentPropertyChanged;
			}
			BunnyServiceSupplier.Default.Services.CollectionChanged += Services_CollectionChanged;
		}

		void Services_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			foreach (var service in e.NewItems.OfType<IService>())
			{
				service.ContentPropertyChanged += service_ContentPropertyChanged;
			}
		}

		#region Private Method
		private IEnumerable<IContentEntity> GetFavorites()
		{
			var appDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Bunny");

			var dbFilePath = Path.Combine(appDir, "database.s3db");

			var conn = new SQLiteConnection(string.Format("Data source={0}", dbFilePath));

			conn.Open();

			var cmd = new SQLiteCommand("SELECT * FROM Labels where auto_type == 0 and deleted == 0", conn);

			var dr = cmd.ExecuteReader();

			while (dr.Read())
			{
				var labelID = dr["label_id"].ToString();
				var labelName = dr["name"].ToString();

				yield return new ContentGroup(labelID, labelName, new Uri(string.Format("c:\\{0}", labelName)), (group, contents) =>
				{
					var conn2 = new SQLiteConnection(string.Format("Data source={0}", dbFilePath));

					conn2.Open();

					var cmd2 = new SQLiteCommand("SELECT * FROM Files t1, LabelFiles t2, Labels t3 where t3.label_id = @labelID and t3.label_id = t2.label_id and t1.file_id = t2.file_id order by t1.event_time asc", conn2);

					cmd2.Parameters.Add(new SQLiteParameter("@labelID", new Guid(labelID)));

					var dr2 = cmd2.ExecuteReader();

					while (dr2.Read())
					{
						var deviceID = dr2["device_id"].ToString();

						var savedPath = dr2["saved_path"].ToString();

						var file = Path.Combine(BunnyDB.ResourceFolder, savedPath);

						var type = ((long)dr2["type"] == 0) ? ContentType.Photo : ContentType.Video;
						contents.Add(new BunnyContent(new Uri(file), dr2["file_id"].ToString(), type));
					}

					conn2.Close();
				});
			}


			conn.Close();
		}
		#endregion



		//TODO: tag & untag 接口一致...

		public void Tag(IEnumerable<IContent> contents, string starredLabelId)
		{
			StationAPI.Tag(string.Join(",", contents.Select(content => content.ID).ToArray()), starredLabelId);
			(m_Favorites.First() as IContentGroup).Refresh();
		}

		public void Tag(IEnumerable<IContent> contents)
		{
			Tag(contents, StarredLabelId);
		}


		public void SaveToFavorite(string favoriteName)
		{
			var labelID = Guid.NewGuid().ToString();
			StationAPI.AddLabel(labelID, favoriteName);

			StationAPI.Tag(string.Join(",", (m_Favorites.First() as IContentGroup).Contents.Select(content => content.ID).ToArray()), labelID);

			//StationAPI.ClearLabel(m_LabelID);

			//m_TaggedContents.Clear();
			m_Favorites.Clear();
			m_Favorites.AddRange(GetFavorites());
		}

		public void SaveToFavorite()
		{
			SaveToFavorite("Untitled Favorite");
		}


		public void UnTag(string contentID)
		{
			UnTag(StarredLabelId, contentID);
		}

		public void UnTag(string labelID , string contentID)
		{
			StationAPI.UnTag(contentID, labelID);
			(m_Favorites.First() as IContentGroup).Refresh();
		}

		public void AddToFavorite(string favoriteID)
		{
			StationAPI.Tag(string.Join(",", (m_Favorites.First() as IContentGroup).Contents.Select(content => content.ID).ToArray()), favoriteID);

			//StationAPI.ClearLabel(m_LabelID);

			//m_TaggedContents.Clear();
			m_Favorites.Clear();
			m_Favorites.AddRange(GetFavorites());
		}

		public void RemoveFavorite(string favoriteID)
		{
			StationAPI.DeleteLabel(favoriteID);

			m_Favorites.Clear();
			m_Favorites.AddRange(GetFavorites());
		}

		public void ClearTaggedContents()
		{
			StationAPI.ClearLabel(StarredLabelId);
		}


		public void OnAir(string labelID, Boolean isOnAir)
		{
			StationAPI.OnAirLabel(labelID, isOnAir);
		}

		void service_ContentPropertyChanged(object sender, ContentPropertyChangeEventArgs e)
		{
			var content = e.Content as IContent;

			if (content.Liked)
			{
				Tag(new IContent[] { content }, StarredLabelId);
			}
			else
				UnTag(StarredLabelId, content.ID);
		}

		public bool IsOnAir(IContentGroup group)
		{
			using (var conn = BunnyDB.CreateConnection())
			{
				conn.Open();
				var cmd = conn.CreateCommand();
				cmd.CommandText = "select on_air from [Labels] where label_id = @label";
				cmd.Parameters.Add(new SQLiteParameter("@label", new Guid(group.ID)));
				var on_air = cmd.ExecuteScalar();

				return on_air != null && (bool)on_air;
			}
		}

		public bool HomeSharingEnabled
		{
			get { return Registry.GetValue(@"HKEY_CURRENT_USER\Software\BunnyHome", "HomeSharing", "true").Equals("true"); }
		}
	}
}
