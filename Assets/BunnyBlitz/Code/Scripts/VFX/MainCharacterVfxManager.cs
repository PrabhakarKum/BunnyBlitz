using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace BunnyBlitz
{
    public class MainCharacterVfxManager : MonoBehaviour
    {
        [SerializeField] private PlayerBehaviour Player;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private VfxGroundTable vfxGroundTable;
        [SerializeField] private VisualEffect vfxFootStep;
        [SerializeField] private VisualEffect vfxJump;
        [SerializeField] private VisualEffect vfxDoubleJump;
        [SerializeField] private VisualEffect vfxLand;
        [SerializeField] private SortingGroup vfxSortingGroup;
    
        private ExposedProperty m_VfxGrounded;
        private ExposedProperty m_VfxGroundNormal;
        private ExposedProperty m_VfxMoveValue;
        private ExposedProperty m_VfxGroundType;
        private ExposedProperty m_FacingRight;
        private VFXEventAttribute m_JumpEventAtt;
        private VFXEventAttribute m_LandEventAtt;

        private float m_WalkingTimer = 0.0f;
        private bool m_IsWalking = false;

        private void Start()
        {
            GameManager.Instance.OnLayerChange += OnLayerChange;
            playerMovement.OnPhysicMaterialChanged += OnPhysicMaterialChanged;
            playerMovement.OnCharacterLanded += OnCharacterLanded;
            playerMovement.OnCharacterJumped += OnCharacterJumped;
            playerMovement.OnMoveStarted += OnMoveActionStarted;
            playerMovement.OnMoveStopped += OnMoveActionStopped;

            Player.OnMaskingChanged += OnMaskingChanged;
        
        
            m_VfxGrounded = "isGrounded";
            m_VfxGroundNormal = "groundNormal";
            m_VfxGroundType = "groundType";
            m_VfxMoveValue = "moveValue";
            m_FacingRight = "facingRight";
            m_JumpEventAtt = vfxJump.CreateVFXEventAttribute();
            m_LandEventAtt = vfxLand.CreateVFXEventAttribute();
        }

        private void OnLayerChange(int sortingLayerID)
        {
            vfxSortingGroup.sortingLayerID = sortingLayerID;
        }

        private void OnMaskingChanged(bool masked)
        {
            if(masked)
                vfxFootStep.Stop();
        }

        private void OnMoveActionStarted()
        {
            if(!Player.SpawnVFX)
                return;
        
            m_IsWalking = true;
            m_WalkingTimer = playerMovement.SteppingTimer;
        
            vfxFootStep.SetBool(m_FacingRight, playerMovement.RigidbodyPlayer.linearVelocityX >= 0);
            vfxFootStep.Play();
        }
        private void OnMoveActionStopped()
        {
            m_IsWalking = false;
            vfxFootStep.Stop();
        }


        private void OnCharacterLanded(Vector2 moveValue, Vector2 groundNormal, PhysicsMaterial2D physMat)
        {
            var landingSound = playerMovement.LandingSound.GetSoundForSurface(playerMovement.CurrentSurfaceType);
            GameManager.Instance.AudioManager.PlaySFXAt(landingSound, transform.position);
        
            OnCharacterEvent(moveValue, groundNormal, physMat, vfxLand, m_LandEventAtt);
        }

        private void OnCharacterJumped(Vector2 moveValue, Vector2 groundNormal, PhysicsMaterial2D physMat)
        {
            if(!Player.SpawnVFX)
                return;
        
            if (playerMovement.IsDoubleJumpPerformed)
            {
                vfxDoubleJump.Play();
            }

            else
            {
                OnCharacterEvent(moveValue, groundNormal, physMat, vfxJump, m_JumpEventAtt);
            }
        
        }
    
        private void OnCharacterEvent(Vector2 moveValue, Vector2 groundNormal, PhysicsMaterial2D physMat, VisualEffect vfx,
            VFXEventAttribute vfxEventAtt)
        {
            if(!Player.SpawnVFX)
                return;
        
            if (physMat?.name != "Spring")
            {
                var s = vfxGroundTable.GetGroundVfxId(physMat);
                vfxEventAtt.SetUint(m_VfxGroundType, s);
                vfx.SetVector2(m_VfxMoveValue, moveValue);
                vfx.SetVector2(m_VfxGroundNormal, groundNormal);
                vfx.Play(vfxEventAtt);
            }
        }
    
        private void OnPhysicMaterialChanged(PhysicsMaterial2D physMat)
        {
            var s = vfxGroundTable.GetGroundVfxId(physMat);
            vfxFootStep.SetUInt(m_VfxGroundType, s);
        }


        void Update()
        {
            vfxFootStep.SetBool(m_FacingRight, playerMovement.RigidbodyPlayer.linearVelocityX >= 0);
            vfxFootStep.SetBool(m_VfxGrounded, playerMovement.IsGrounded);

            if (m_IsWalking && playerMovement.IsGrounded)
            {
                //TODO : this is all hardcoded, based on the animator that blend running from velocity 6 to velocity 12
                float speedRatio = Mathf.Clamp01((Mathf.Abs(playerMovement.RigidbodyPlayer.linearVelocityX) - 6.0f) / 6.0f);
                m_WalkingTimer += Time.deltaTime * speedRatio;
                if (m_WalkingTimer > playerMovement.SteppingTimer)
                {
                    m_WalkingTimer = 0.0f;
                    var stepSound = Player.IsInWater ? playerMovement.WaterSteppingSound : playerMovement.StepSound.GetSoundForSurface(playerMovement.CurrentSurfaceType);
                    GameManager.Instance.AudioManager.PlaySFXAt(stepSound, transform.position);
                }
            }
        }

        void OnDestroy()
        {
            playerMovement.OnPhysicMaterialChanged -= OnPhysicMaterialChanged;
            playerMovement.OnCharacterLanded -= OnCharacterLanded;
            playerMovement.OnCharacterJumped -= OnCharacterJumped;
            GameManager.Instance.OnLayerChange -= OnLayerChange;
        }
    }
}