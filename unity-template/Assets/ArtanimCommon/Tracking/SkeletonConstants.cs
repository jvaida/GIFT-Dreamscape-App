using Artanim.Location.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Artanim.Tracking
{
	public static class SkeletonConstants
	{
		public const char RigidbodyPrefixDelimitor = ',';
		private const string SkeletonRigidbodyRegEx = "[a-z|A-Z|0-9]$"; //Ends with alpha character

		static string[] _pelvisPrefixes;

		static string[] PelvisPrefixes
		{
			get
			{
				if (_pelvisPrefixes == null)
				{
					_pelvisPrefixes = ReadPrefixes(ConfigService.Instance.Config.Location.IKServer.Skeleton.PelvisPrefix);
				}
				return _pelvisPrefixes;
			}
		}

		static string[] _headPrefixes;

		static string[] HeadPrefixes
		{
			get
			{
				if (_headPrefixes == null)
				{
					_headPrefixes = ReadPrefixes(ConfigService.Instance.Config.Location.IKServer.Skeleton.HeadPrefix);
				}
				return _headPrefixes;
			}
		}

		static string[] _footLeftPrefixes;

		static string[] FootLeftPrefixes
		{
			get
			{
				if (_footLeftPrefixes == null)
				{
					_footLeftPrefixes = ReadPrefixes(ConfigService.Instance.Config.Location.IKServer.Skeleton.LeftFoot);
				}
				return _footLeftPrefixes;
			}
		}

		static string[] _footRightPrefixes;

		static string[] FootRightPrefixes
		{
			get
			{
				if (_footRightPrefixes == null)
				{
					_footRightPrefixes = ReadPrefixes(ConfigService.Instance.Config.Location.IKServer.Skeleton.RightFoot);
				}
				return _footRightPrefixes;
			}
		}

		static string[] _handLeftPrefixes;

		static string[] HandLeftPrefixes
		{
			get
			{
				if (_handLeftPrefixes == null)
				{
					_handLeftPrefixes = ReadPrefixes(ConfigService.Instance.Config.Location.IKServer.Skeleton.LeftHand);
				}
				return _handLeftPrefixes;
			}
		}

		static string[] _handRightPrefixes;

		static string[] HandRightPrefixes
		{
			get
			{
				if (_handRightPrefixes == null)
				{
					_handRightPrefixes = ReadPrefixes(ConfigService.Instance.Config.Location.IKServer.Skeleton.RightHand);
				}
				return _handRightPrefixes;
			}
		}

		static string[] _rigidbodyNamePrefixes;

		static string[] RigidbodyNamePrefixes
		{
			get
			{
				if (_rigidbodyNamePrefixes == null)
				{
					var list = new List<string>();
					list.AddRange(HeadPrefixes);
					list.AddRange(PelvisPrefixes);
					list.AddRange(FootLeftPrefixes);
					list.AddRange(FootRightPrefixes);
					list.AddRange(HandLeftPrefixes);
					list.AddRange(HandRightPrefixes);
					_rigidbodyNamePrefixes = list.ToArray();

					//Debug.LogErrorFormat("All prefixes: {0}", string.Join(", ", _rigidbodyNamePrefixes));
				}
				return _rigidbodyNamePrefixes;
			}
		}

        public static ESkeletonSubject MainSkeletonSubject
		{
			get { return ESkeletonSubject.Head; }
		}

		public static bool IsSkeletonMainSubject(string rigidbodyName)
		{
			return IsHeadSubject(rigidbodyName);
		}

		public static bool IsHeadSubject(string rigidbodyName)
		{
            return IsPrefixInList(HeadPrefixes, rigidbodyName);
		}

		public static bool IsPelvisSubject(string rigidbodyName)
		{
            return IsPrefixInList(PelvisPrefixes, rigidbodyName);
		}

		public static bool IsFootLeftSubject(string rigidbodyName)
		{
            return IsPrefixInList(FootLeftPrefixes, rigidbodyName);
		}

		public static bool IsFootRightSubject(string rigidbodyName)
		{
            return IsPrefixInList(FootRightPrefixes, rigidbodyName);
		}

		public static bool IsHandLeftSubject(string rigidbodyName)
		{
            return IsPrefixInList(HandLeftPrefixes, rigidbodyName);
		}

		public static bool IsHandRightSubject(string rigidbodyName)
		{
            return IsPrefixInList(HandRightPrefixes, rigidbodyName);
		}

		public static bool IsSkeletonRigidbody(string rigidbodyName)
		{
			//Check name
			var regEx = new Regex(SkeletonRigidbodyRegEx);
			if(regEx.IsMatch(rigidbodyName))
            {
				foreach (var prefix in RigidbodyNamePrefixes)
				{
					if (rigidbodyName.StartsWith(prefix))
						return true;
				}
			}
			
			return false;
		}

		public static string GetSkeletonShortnameFromRigidbody(string rigidbodyName)
		{
			if(IsSkeletonRigidbody(rigidbodyName))
			{
				var elements = rigidbodyName.Split('_');
				return elements.Last();
			}
			return string.Empty;
		}

		public static ESkeletonSubject GetSkeletonSubject(string rigidbodyName)
		{
			if (IsPelvisSubject(rigidbodyName))
				return ESkeletonSubject.Pelvis;
			else if (IsHeadSubject(rigidbodyName))
				return ESkeletonSubject.Head;
			else if (IsFootLeftSubject(rigidbodyName))
				return ESkeletonSubject.FootLeft;
			else if (IsFootRightSubject(rigidbodyName))
				return ESkeletonSubject.FootRight;
			else if (IsHandLeftSubject(rigidbodyName))
				return ESkeletonSubject.HandLeft;
			else if (IsHandRightSubject(rigidbodyName))
				return ESkeletonSubject.HandRight;
			else
				throw new ArgumentException(rigidbodyName);
		}

        public static string[] GetSkeletonSubjectPrefixes(ESkeletonSubject skeletonSubject)
        {
            switch (skeletonSubject)
            {
                case ESkeletonSubject.Head:
                    return SkeletonConstants.HeadPrefixes;
                case ESkeletonSubject.Pelvis:
                    return SkeletonConstants.PelvisPrefixes;
                case ESkeletonSubject.HandRight:
                    return SkeletonConstants.HandRightPrefixes;
                case ESkeletonSubject.HandLeft:
                    return SkeletonConstants.HandLeftPrefixes;
                case ESkeletonSubject.FootRight:
                    return SkeletonConstants.FootRightPrefixes;
                case ESkeletonSubject.FootLeft:
                    return SkeletonConstants.FootLeftPrefixes;
                default:
                    throw new InvalidOperationException("Unsupported skeleton subject: " + skeletonSubject);
            }
        }

        static string[] _legacyNames = { "Hand_L", "Hand_R", "Foot_L", "Foot_R" };

		static string[] ReadPrefixes(string prefixList)
		{
			var prefixes = prefixList.Split(RigidbodyPrefixDelimitor);
			for (var i = 0; i < prefixes.Length; ++i)
			{
				string pref = prefixes[i];
				if (pref.Contains('_') && (!_legacyNames.Contains(pref)))
					Debug.LogWarning("A skeleton's rigibody prefix has one more underscore which is a reserved character: " + prefixes[i]);
				prefixes[i] = pref.Trim() + "_";
			}
			return prefixes;
		}

        private static bool IsPrefixInList(string[] list, string name)
        {
            for(var i=0; i < list.Length; ++i)
            {
                if (name.StartsWith(list[i]))
                    return true;
            }

            return false;
        }
    }
}