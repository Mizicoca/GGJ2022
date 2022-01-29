using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    public GameObject CameraBoundsCube;

    [SerializeField]
    public EmoteTextRenderer ChooseActionRenderer;

    [SerializeField]
    public RectTransform ActionPanel;

    [SerializeField]
    public string ForwardsBackwardsMovementKey = "Vertical";

    [SerializeField]
    public string LeftRightMovementKey = "Horizontal";

    [SerializeField]
    public GameObject StakeConfirmationGo;

    [SerializeField]
    public Text StakeText;

    private PhysicalCharacter CurrentlySelectedCharacter;

    public float SpeedHorizontal = 0.2f;
    public float SpeedVertical = 0.2f;

    public float FallOffSpeed = 10.0f;

    private Vector3 startingPosition = new Vector3();
    private Vector3 leftOverVelocity = new Vector3();

    private Vector2? vActionPanelOriginalPositon = null;

    private Vector2 vMinBounds;
    private Vector2 vMaxBounds;

    private bool bPopulatedActionEmotes = false;
    private bool isStakeConfirmationWindowUp = false;

    public bool CanMoveCamera()
    {
        if(Service.Player.IsTalkingToCharacter)
        {
            return false;
        }

        if(isStakeConfirmationWindowUp)
        {
            return false;
        }

        return true;
    }

    public void PickedTalkAction()
    {
        Debug.Assert(CurrentlySelectedCharacter);
        Debug.Log(string.Format("Picked talk to {0}", CurrentlySelectedCharacter.name));

        Service.Player.IsTalkingToCharacter = true;
    }

    public void PickedStakeAction()
    {
        Debug.Assert(CurrentlySelectedCharacter);
        Debug.Log(string.Format("Picked stake {0}", CurrentlySelectedCharacter.name));

        Service.Game.ProcessPlayerActionStakeCharacter(CurrentlySelectedCharacter.AssociatedCharacter);
    }

    public void ShowStakeConfirmation()
    {
        isStakeConfirmationWindowUp = true;

        StakeConfirmationGo.SetActive(true);
        Service.Audio.PlayUIClick();
        ActionPanel.gameObject.SetActive(false);
    }

    public void CancelledStakeAction()
    {
        isStakeConfirmationWindowUp = false;

        StakeConfirmationGo.SetActive(false);
        Service.Audio.PlayUIClick();
        CurrentlySelectedCharacter = null;
    }

    public void HideActionPanel()
    {
        Service.Audio.PlayUIClick();
        CurrentlySelectedCharacter = null;
        ActionPanel.gameObject.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        startingPosition = transform.position;

        Debug.Assert(CameraBoundsCube);
        if(CameraBoundsCube)
        {
            vMinBounds = new Vector2(CameraBoundsCube.transform.position.x - (CameraBoundsCube.transform.localScale.x / 2),
                                     CameraBoundsCube.transform.position.z - (CameraBoundsCube.transform.localScale.z / 2));
            vMaxBounds = new Vector2(CameraBoundsCube.transform.position.x + (CameraBoundsCube.transform.localScale.x / 2),
                                     CameraBoundsCube.transform.position.z + (CameraBoundsCube.transform.localScale.z / 2));
        }
        else
        {
            vMinBounds = new Vector2(-50f, -50f);
            vMinBounds = new Vector2(50f,50f);
        }

        ActionPanel.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Service.Game.CurrentState == WerewolfGame.GameState.PlayerInvestigateDay
            || Service.Game.CurrentState == WerewolfGame.GameState.PlayerInvestigateNight)
        {
            ProcessUpdateInit();
            ProcessMovement();
            ProcessClicking();
            ProcessUpdateActionUIPosition();
        }
    }

    void ProcessUpdateInit()
    {
        if (!bPopulatedActionEmotes)
        {
            bPopulatedActionEmotes = true;
            ChooseActionRenderer.ClearEmotes(true);
            ChooseActionRenderer.AddEmoteToRender(Service.InfoManager.EmoteMapBySubType[Emote.EmoteSubType.Specific_TalkAction]);
            ChooseActionRenderer.AddEmoteToRender(Service.InfoManager.EmoteMapBySubType[Emote.EmoteSubType.Specific_StakeAction]);
            ChooseActionRenderer.RenderWithoutEmotes();
            ChooseActionRenderer.SortEmotes(125f);

            ActionPanel.gameObject.SetActive(false);
        }
    }

    void ProcessUpdateActionUIPosition()
    {
        if(!CurrentlySelectedCharacter)
        {
            return;
        }

        var pos = GetUIPositionForCharacterAction();

        // Clear when the camera moves away
        if(Vector2.Distance(pos, vActionPanelOriginalPositon.Value) > 100.0f)
        {
            HideActionPanel();
            return;
        }

        ActionPanel.anchoredPosition = new Vector2(pos.x, pos.y);
    }

    void ProcessClicking()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100))
            {
                Debug.Log(hit.transform.gameObject.name);

                PhysicalCharacter pc = hit.transform.gameObject.GetComponent<PhysicalCharacter>();
                if(pc)
                {
                    CurrentlySelectedCharacter = pc;

                    var pos = GetUIPositionForCharacterAction();

                    vActionPanelOriginalPositon = new Vector2(pos.x, pos.y);

                    ActionPanel.anchoredPosition = pos;
                    ActionPanel.gameObject.SetActive(true);
                }
            }
        }
    }

    void ProcessMovement()
    {
        if (CanMoveCamera())
        {
            if (Input.GetButton(ForwardsBackwardsMovementKey) || Input.GetButton(LeftRightMovementKey))
            {
                float vert = Input.GetAxis(ForwardsBackwardsMovementKey) * SpeedVertical;
                float hori = Input.GetAxis(LeftRightMovementKey) * SpeedHorizontal;

                leftOverVelocity += new Vector3(hori, 0, vert) * Time.deltaTime;
            }
        }

        if (transform.position.x <= vMinBounds.x)
        {
            if (leftOverVelocity.x < 0)
            {
                leftOverVelocity.x = 0;
            }
        }
        else if (transform.position.x >= vMaxBounds.x)
        {
            if (leftOverVelocity.x > 0)
            {
                leftOverVelocity.x = 0;
            }
        }

        if (transform.position.z <= vMinBounds.y)
        {
            if (leftOverVelocity.z < 0)
            {
                leftOverVelocity.z = 0;
            }
        }
        else if (transform.position.z >= vMaxBounds.y)
        {
            if (leftOverVelocity.z > 0)
            {
                leftOverVelocity.z = 0;
            }
        }

        transform.position += leftOverVelocity;
        leftOverVelocity = Vector3.Lerp(leftOverVelocity, Vector3.zero, FallOffSpeed * Time.deltaTime);
    }

    Vector2 GetUIPositionForCharacterAction()
    {
        Vector2 viewportPos = Camera.main.WorldToViewportPoint(CurrentlySelectedCharacter.transform.position);
        Vector2 screenPos = new Vector2(
        (viewportPos.x * Screen.width) - (Screen.width * 0.5f),
        (viewportPos.y * Screen.height) - (Screen.height * 0.5f));

        return screenPos;
    }
}
