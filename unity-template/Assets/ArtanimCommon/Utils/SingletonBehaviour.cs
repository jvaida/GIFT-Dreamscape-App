using UnityEngine;
using System.Collections;

namespace Artanim
{

    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
    {
	    private static T _instance;

		/// <summary>
		/// Returns whether of not the singleton was found in a previous call to <see cref="Instance"/>
		/// This is just an optimized way to check for the singleton (without searching for it) in the de-init code.
		/// </summary>
	    public static bool HasInstance
	    {
		    get
		    {
			    return _instance != null;
		    }
	    }

		/// <summary>
		/// Search for the first instance of the <see cref="T"/> type and returns it.
		/// The instance is stored so subsequent calls have no CPU cost.
		/// </summary>
		public static T Instance
	    {
		    get
		    {
			    if(_instance == null)
			    {
					_instance = FindObjectOfType<T>();
                }
                return _instance;
		    }

		    protected set
		    {
			    _instance = value;
		    }
	    }
	
    }

}