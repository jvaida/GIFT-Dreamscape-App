using Artanim.Location.Messages;
using System.Collections;

namespace Artanim
{
	public interface ICameraFader
	{
		/// <summary>
		/// Request the current fade state of the camera fader. This method is called by the SceneController when the camera fader is replaced during a scene load.
		/// </summary>
		/// <returns>Current transition of the camera fader</returns>
		Transition GetTragetTransition();

		/// <summary>
		/// This async method is called by the SceneController when the player view must be faded out.
		/// Returning from this method means that the screen is fully faded out and scene transitions can be done without affecting the players view.
		/// </summary>
		/// <param name="transition">The transition requested by the experience.</param>
		/// <param name="customTransitionName">Name of the custom transition if Transition is set to Custom.</param>
		IEnumerator DoFadeAsync(Transition transition, string customTransitionName = null);

		/// <summary>
		/// Method called by the SceneController when the players view can be faded-in again after a scene load.
		/// Returning from this method means that the players view is fully faded-in.
		/// </summary>
		/// <returns></returns>
		IEnumerator DoFadeInAsync();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="transition">The transition requested by the experience.</param>
		/// /// <param name="customTransitionName">Name of the custom transition if Transition is set to Custom.</param>
		void SetFaded(Transition transition, string customTransitionName = null);

	}
}