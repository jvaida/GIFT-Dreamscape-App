using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{
	public class DefaultExperienceSetup : MonoBehaviour, IExperienceSetup
	{
		[SerializeField]
		InputField _domainIdInput = null;

		[SerializeField]
		Text _domainIdError = null;

		[SerializeField]
		InputField _ipInput = null;

		[SerializeField]
		Text _ipInputError = null;

		[SerializeField]
		Color _inputFieldsErrorColor = Color.red;

		bool _ready;

		Color _domainIdColor, _ipColor;

		uint _maxDomainId;
		uint _domainId = uint.MaxValue;
		List<System.Net.IPAddress> _componentsIps = new List<System.Net.IPAddress>();

		void Start()
        {
			_maxDomainId = NetBus.NetBus.MaximumDomainId;
			_domainIdColor = _domainIdInput.GetComponent<Image>().color;
			_ipColor = _ipInput.GetComponent<Image>().color;

			int domainId = PlayerPrefs.GetInt(RuntimeLocationComponent.KEY_DOMAIN_ID);
			_domainIdInput.text = domainId >= 0 ? domainId.ToString() : "";
			_ipInput.text = PlayerPrefs.GetString(RuntimeLocationComponent.KEY_INITIAL_PEERS).ToString();
		}

		public IEnumerator Run(ExperienceSetupSettings outSettings)
        {
			yield return new WaitUntil(() => _ready);

			outSettings.DomainId = _domainId;
			outSettings.ComponentsIps = _componentsIps;
		}

		public void ValidateDomainId(string text)
        {
			string error = null;
			if (!string.IsNullOrEmpty(text))
            {
				int domainId;
				if (int.TryParse(text, out domainId) && (domainId >= 0) && (domainId <= _maxDomainId))
                {
					_domainId = (uint)domainId;
				}
				else
                {
					error = string.Format("Domain Id needs to be a valid number between 0 and {0} included", _maxDomainId);
				}
			}

			_domainIdInput.GetComponent<Image>().color = error == null ? _domainIdColor : _inputFieldsErrorColor;
			_domainIdError.text = error;
		}

		public void ValidateIPs(string text)
        {
			string error = null;

			_componentsIps.Clear();
			foreach (var ipString in text.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)))
			{
				try
				{
					_componentsIps.Add(System.Net.IPAddress.Parse(ipString));
				}
				catch (System.FormatException e)
				{
					error = e.Message;
				}
			}

			_ipInput.GetComponent<Image>().color = error == null ? _ipColor : _inputFieldsErrorColor;
			_ipInputError.text = error;
		}

		public void StartExperience()
        {
			_ready = true;
		}

		public void Quit()
        {
			Application.Quit();
        }
	}
}