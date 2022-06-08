using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{

    public class StandaloneHandPopupUI : MonoBehaviour
    {
        private const string TRIGGER_BLINK = "Blink";

        public enum EHand { Left, Right, }
        public enum ENavigation { None, Up, Down, }

        public EHand Hand;
        public GameObject PopupRoot;
        public GameObject ActionsRoot;

        public Text TextHeader;
        public Text TextAction;

        public Animator SelectionAnimator;

        public List<IStandalonePopupAction> Actions = new List<IStandalonePopupAction>();
        public int CurrentSelectedActionIndex;
        public bool ActionSelected;

        private Location.Messages.EAvatarBodyPart BodyPart;
        private bool CanNavigate;

        private string AxisOpenClose;
        private string ButtonSelect;
        private string AxisNavigate;

        private void Start()
        {
            AxisOpenClose = Hand == EHand.Left ? DevelopmentMode.AXIS_STANDALONE_POPUP_LEFT : DevelopmentMode.AXIS_STANDALONE_POPUP_RIGHT;
            ButtonSelect = Hand == EHand.Left ? DevelopmentMode.BUTTON_STANDALONE_POPUP_LEFT_SELECT : DevelopmentMode.BUTTON_STANDALONE_POPUP_RIGHT_SELECT;
            AxisNavigate = Hand == EHand.Left ? DevelopmentMode.AXIS_STANDALONE_POPUP_LEFT_VERTICAL : DevelopmentMode.AXIS_STANDALONE_POPUP_RIGHT_VERTICAL;
            BodyPart = Hand == EHand.Left ? Location.Messages.EAvatarBodyPart.LeftHand : Location.Messages.EAvatarBodyPart.RightHand;

            //Init actions
            if(ActionsRoot)
            {
                foreach (var action in ActionsRoot.transform.GetComponentsInChildren<IStandalonePopupAction>())
                    Actions.Add(action);
            }
        }

        private void LateUpdate()
        {
            if (PopupRoot)
            {
                //Show/Hide
                var show = DevelopmentMode.IsAxisDown(AxisOpenClose);
                if(show != PopupRoot.activeInHierarchy)
                {
                    PopupRoot.SetActive(show);

                    if (show)
                        ResetActionSelection();
                }

                if (PopupRoot.activeInHierarchy)
                {
                    //Position UI
                    var leftHand = GameController.Instance.CurrentPlayer.AvatarController.GetAvatarBodyPart(BodyPart);
                    transform.position = leftHand.transform.position;
                    transform.rotation = leftHand.transform.rotation;


                    //Action navigation
                    switch (GetNavigation())
                    {
                        case ENavigation.Up:

                            if(!ActionSelected)
                            {
                                CurrentSelectedActionIndex++;
                                if (CurrentSelectedActionIndex == Actions.Count) CurrentSelectedActionIndex = 0;
                            }
                            else
                            {
                                Actions[CurrentSelectedActionIndex].NextItem();
                            }

                            break;
                        case ENavigation.Down:

                            if(!ActionSelected)
                            {
                                CurrentSelectedActionIndex--;
                                if (CurrentSelectedActionIndex < 0) CurrentSelectedActionIndex = Actions.Count - 1;
                            }
                            else
                            {
                                Actions[CurrentSelectedActionIndex].PrevItem();
                            }

                            break;
                    }

                    //Selection
                    if(Input.GetButtonUp(ButtonSelect))
                    {
                        if(!ActionSelected)
                        {
                            SelectCurrentAction();
                        }
                        else
                        {
                            if (SelectionAnimator)
                                SelectionAnimator.SetTrigger(TRIGGER_BLINK);

                            //Execute action
                            Actions[CurrentSelectedActionIndex].ExecuteCurrentItem();
                        }
                    }

                    ShowCurrentAction();
                }
            }
        }

        private void SelectCurrentAction()
        {
            ActionSelected = true;

            //Disable action text, let action handle display
            if (TextAction) TextAction.enabled = false;

            //Enable actions GO
            Actions[CurrentSelectedActionIndex].Init();
        }

        private ENavigation GetNavigation()
        {
            var actionNavAxis = DevelopmentMode.GetAxis(AxisNavigate);
            if (CanNavigate && actionNavAxis != 0f)
            {
                CanNavigate = false;

                if (actionNavAxis > 0)
                    return ENavigation.Up;
                else
                    return ENavigation.Down;
            }
            else if (actionNavAxis == 0f)
            {
                CanNavigate = true;
            }
            return ENavigation.None;
        }
        

        private void ShowCurrentAction()
        {
            //Header
            if(TextHeader)
            {
                TextHeader.enabled = ActionSelected;
                if (ActionSelected)
                    TextHeader.text = Actions[CurrentSelectedActionIndex].Header;
            }

            //Action
            if (TextAction)
            {
                if(!ActionSelected)
                {
                    TextAction.text = Actions[CurrentSelectedActionIndex].Header;
                }
            }
        }

        private void ResetActionSelection()
        {
            if (ActionsRoot)
            {
                if(Actions.Count > 1)
                {
                    //Hide all root childs
                    for (var i = 0; i < ActionsRoot.transform.childCount; ++i)
                    {
                        ActionsRoot.transform.GetChild(i).gameObject.SetActive(false);
                    }

                    ActionSelected = false;
                    if (TextAction) TextAction.enabled = true;
                }
                else
                {
                    SelectCurrentAction();
                }
            }
        }
    }

}