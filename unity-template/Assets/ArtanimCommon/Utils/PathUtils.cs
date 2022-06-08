using Artanim.Location.Data;
using Artanim.Location.SharedData;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Artanim
{
	public static class PathUtils
	{
		/// <summary>
		/// Returns a path where to write files for the current session
		/// </summary>
		/// <returns>A path unique to the session and player</returns>
		public static string GetSessionFilesPath()
		{
			if (GameController.Instance == null)
			{
				throw new Exception("GameController not found");
			}

			var session = GameController.Instance.CurrentSession;
			if (session == null)
			{
				throw new Exception("No active session");
			}

			// Session's folder
			string sessionId = string.IsNullOrEmpty(session.ApiSessionId) ? session.SharedId.Guid.ToString() : session.ApiSessionId;
			string sessionDir = string.Format("session-{0}-{1}", session.CreationTime.ToLocalTime(), sessionId);
			sessionDir = ReplaceInvalidCharactersInPath(sessionDir);

			// Component's folder
			var player = GameController.Instance.CurrentPlayer;
			string component = player != null ? "client" : "server";
			var guid = (player != null) && (player.Player.UserSessionId != Guid.Empty) ? player.Player.UserSessionId : SharedDataUtils.GetMyComponent<LocationComponent>().SharedId.Guid;
			string componentDir = string.Format("{0}-{1}", component, guid);
			componentDir = ReplaceInvalidCharactersInPath(componentDir);

			// Create path
			return System.IO.Path.Combine(Utils.Paths.AppDataDir, System.IO.Path.Combine(sessionDir, componentDir));
		}

		/// <summary>
		/// Returns a path where to write a file for the current session
		/// </summary>
		/// <param name="filename">Name of the file to write</param>
		/// <returns>The full pathname</returns>
		/// <remarks>This method also removes invalid path characters</remarks>
		public static string GetSessionFilePathname(string filename)
		{
			return System.IO.Path.Combine(GetSessionFilesPath(), ReplaceInvalidCharactersInPath(filename));
		}

		/// <summary>
		/// Replace invalid path characters by the given string, also replace spaces
		/// </summary>
		/// <param name="path">Some path</param>
		/// <returns>Valid path</returns>
		public static string ReplaceInvalidCharactersInPath(string path)
		{
			return Utils.Paths.ReplaceInvalidCharacters(path, "_").Replace(" ", "_");
		}
	}
}