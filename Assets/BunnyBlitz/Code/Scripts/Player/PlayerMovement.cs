using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace BunnyBlitz
{
    public class PlayerMovement : MonoBehaviour, ConveyorScript2D.IConveyorTarget
    {
        public Rigidbody2D RigidbodyPlayer { get; private set; }

        public SurfaceType CurrentSurfaceType { get; private set; }

        void ConveyorScript2D.IConveyorTarget.AddImpulse(Vector2 impulse) => conveyorImpulse += impulse;

        [Header("Character movement")]
        public bool IsGrounded;
        public bool IsSliding;
        public bool IsCeiling;
        public bool IsDoubleJumpPerformed;
        public GameObject currentMovingPlatform;
        public Vector3 currentMovingPlatPrevFramePosition;
        public float jumpForce = 8f;
        public float jumpForceLongPress = 0.05f;
        public float jumpLongPressAcceleration = 5;
        public float jumpDoubleJumpForce = 2f;
        public float bounceForce = 20f;
        public float accelerationXSpeed = 42;
        public float accelerationXSpeedOnAir = 20;
        //public ContactFilter2D FilterGroundContact = new ContactFilter2D();
        public Vector2 groundNormal;
        public LayerMask groundLayers;
        public float pressedJumpTime;
        public float gravityScaleWhenFallingDown;
        public bool wasJumpButtonReleased = true;
        public float timeLongPress = 0.3f;
        public float XspeedLimit = 12f;
        public float YspeedLimit = 12f;

        [Range(0f, 90f)]
        public float angleGroundReq = 68f;

        //[Range(0f, 90f)]
        //public float angleGroundAntiSlidingReq = 30f;

        [Range(0f, 90f)]
        public float ceilingAngleReq = 80f;

        // Conveyor impulses.
        public Vector2 conveyorImpulse;    

        //private bool IsTouchingCeiling;
        [Range(1f, 1.6f)]
        public float boostSteepGroundMultiplier = 1.2f;
        [Range(0f, 1f)] //0 no linear X after bouncing on block or enemy, 1f is the same linear X after bounce
        public float multiplierReduceLinearXafterBounce = 0.6f;

        [Header("In Portal Data")]
        public float PortalXSpeedLimit = 5.0f;
        public float PortalYSpeedLimit = 5.0f;

        [Header("Character props")]
        public float springImpulse;


        [Header("Animations")]
        public Transform modelHolder;
        public float modelInclinationOnGround = 0.5f;
        public float modelFacingDir;
        public float rotationLerpFactor = 0.9f;
        public float delayShootingProjectile = 0.2f; //to match the animation

        // Set to true to ignore input and only drive animation through velocity, used when moving character by script
        public bool IsScriptDriven { get; set; } = false;

        private int countOnAirChecks;
        public int countOnAirChecksReqForAnim = 20;
        public CinemachineCamera CloseUpCam; //used to change the priority of this cam vs the far one based on speed of the character
        
        [Header("Debug")]
        public bool IsDrawingDebugInfo = true;
        public bool IsTrailRendererOn = true;

        public event Action OnMoveStarted;
        public event Action OnMoveStopped;
        public event Action<PhysicsMaterial2D> OnPhysicMaterialChanged;
        public event Action<Vector2, Vector2, PhysicsMaterial2D> OnCharacterLanded;
        public event Action<Vector2, Vector2, PhysicsMaterial2D> OnCharacterJumped;
        
        [Header("Audio")]
        public AudioResource JumpAudio;
        public AudioResource DoubleJumpAudio;

        public AudioManager.SurfaceSoundMatchUp LandingSound;
        public AudioManager.SurfaceSoundMatchUp StepSound;
        public AudioResource WaterSteppingSound;
        public float SteppingTimer = 1.0f;

        private Vector2 m_moveValue;
        private Vector2 infoContactPoint;
        private float infoContactPointDegrees;

        private PlayerInput m_PlayerInput;
        private PlayerBehaviour m_PlayerBehaviour;

        private float m_CurrentXLimit = 0.0f;
        private float m_CurrentYLimit = 0.0f;

        private float m_TargetFPS = 60f; //used to calculate forces
        private float m_OnAirLinearX;
        //public float decelerationXvalue = 0.2f;
        private float m_GravityScaleNormal;
        private ContactFilter2D m_ContactFilter;
        private List<ContactPoint2D> m_Contacts = new();
        private bool isMoving = false;
        private PhysicsMaterial2D currentPhysMaterial;
        private Collider2D m_TouchedOneWayCollider = null;

        private float m_TimeSinceBounced = 0.0f;
    
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            m_GravityScaleNormal = RigidbodyPlayer.gravityScale;
            // Configure the contact filter basics.
            m_ContactFilter = new ContactFilter2D
            {
                useTriggers = false,
                useNormalAngle = true,
                useLayerMask = true,
                layerMask = groundLayers
            };
            if (TryGetComponent<TrailRenderer>(out TrailRenderer tr))
            {
                if (IsTrailRendererOn)
                {
                    tr.enabled = true;
                }
                else
                {
                    tr.enabled = false;
                }
            }

            OnMaskingChanged(false);
        }

 

        private void UpdateContactState()
        {
            CurrentSurfaceType = null;
            m_TouchedOneWayCollider = null;
            
            bool hasLanded = false;
            
            // Grounded.
            // NOTE: Normal "up" is +90 deg.
            m_ContactFilter.SetNormalAngle(90f - angleGroundReq, 90f + angleGroundReq);
            IsGrounded = RigidbodyPlayer.IsTouching(m_ContactFilter);
            
            if (IsGrounded && countOnAirChecks > countOnAirChecksReqForAnim)
            {
                //Updates to animation controller
                if (m_PlayerBehaviour.Animator != null)
                {
                    m_PlayerBehaviour.Animator.SetTrigger("hasLanded");
                    hasLanded = true;
                }
            }
            
            if (IsGrounded)
            {
                // NOTE: This assumes the contact filter is still configured to detect the ground.
                // There can be multiple contacts here!
                RigidbodyPlayer.GetContacts(m_ContactFilter, m_Contacts);
                groundNormal = Vector2.down; //let's choose the contact normal closest to Vector2.up
                PhysicsMaterial2D mat = null;
                foreach (var contact in m_Contacts)
                {
                    //Debug.Log($"Ground Normal: {contact.normal}");
                    if (Vector2.Dot(contact.normal, Vector2.up) > Vector2.Dot(groundNormal, Vector2.up))
                    {
                        groundNormal = contact.normal;

                        var tm = contact.collider.GetComponentInChildren<Tilemap>();
                        if (tm != null)
                        {
                            var tile = tm.GetTile(tm.WorldToCell(contact.point - contact.normal * 0.5f));
                            if (GameManager.Instance.TileToSurfaceTypeLookup.GetSurfaceForTile(tile, out var type))
                            {
                                CurrentSurfaceType = type;
                            }
                        }
                    
                        // the Current RelativeXSpeed between Player and ContactPoint2D
                        var currentRelativeSpeedX = MathF.Abs(contact.relativeVelocity.x);
                    
                        if (currentRelativeSpeedX > 0.01f && !isMoving)
                        {
                            isMoving = true;
                            OnMoveStarted?.Invoke();
                        }

                        if (currentRelativeSpeedX < 0.01f && isMoving)
                        {
                            isMoving = false;
                            OnMoveStopped?.Invoke();
                        }
                    
                        if (contact.collider.sharedMaterial != null)
                        {
                            mat = contact.collider.sharedMaterial;
                       
                        }

                        var platformEffector = contact.collider.GetComponent<PlatformEffector2D>();
                        if (platformEffector != null && platformEffector.useOneWay)
                        {
                            m_TouchedOneWayCollider = contact.collider;
                        }
                    }
                }

                if (mat != currentPhysMaterial)
                {
                    currentPhysMaterial = mat;
                    OnPhysicMaterialChanged?.Invoke(currentPhysMaterial);
                }
            

                IsDoubleJumpPerformed = false;
                //for anim
                countOnAirChecks = 0;
            }
            else
            {
                //for anim
                countOnAirChecks++;
            }
            if (hasLanded)
                OnCharacterLanded?.Invoke(m_moveValue, groundNormal, currentPhysMaterial);

            // Ceiling.
            m_ContactFilter.SetNormalAngle(270f - ceilingAngleReq, 270f + ceilingAngleReq);
            IsCeiling = RigidbodyPlayer.IsTouching(m_ContactFilter);
        }
        #region Update
        // Update is called once per frame
        void Update()
        {
            UpdateContactState();

            if (Time.timeScale < 0.001f)
                return;

            m_moveValue = m_PlayerInput.Move.ReadValue<Vector2>();
        
            if (IsGrounded)
            {
                RigidbodyPlayer.gravityScale = m_GravityScaleNormal;

                float absVelocityX = Mathf.Abs(RigidbodyPlayer.linearVelocityX);
                if (m_moveValue.x == 0 && absVelocityX > 0.1 && RigidbodyPlayer.linearVelocityY < 0.1)
                {
                    IsSliding = true;
                }
                else
                {
                    IsSliding = false;
                }
            
                if (m_PlayerInput.Jump.WasPressedThisFrame())
                {
                    if (m_moveValue.y < -0.1f && m_TouchedOneWayCollider != null)
                    {
                        m_PlayerBehaviour.IgnoreCollider(m_TouchedOneWayCollider, 0.3f); 
                    }
                    else
                    {
                        GameManager.Instance.AudioManager.PlaySFXAt(JumpAudio, transform.position);
                        IsGrounded = false;
                        //Debug.Log($"Jumped, is grounded: {IsGrounded}");
                        wasJumpButtonReleased = false;
                        RigidbodyPlayer.linearVelocityY = 0; //reset Y velocity so ramp boosting force doesn't affect
                        RigidbodyPlayer.AddForceY(jumpForce, ForceMode2D.Impulse);
                        pressedJumpTime = Time.time;
                        OnCharacterJumped?.Invoke(m_moveValue, groundNormal, currentPhysMaterial);
                        //Updates to animation controller
                        if (m_PlayerBehaviour.Animator != null)
                        {
                            //Debug.Log("jumping trigger");
                            m_PlayerBehaviour.Animator.SetTrigger("hasJumped");
                        }
                    }
                }
            }
            else
            {
                m_TimeSinceBounced += Time.deltaTime;

                //in the air
                // We wait at least 0.1s after a bounce (on enemy or spring) to double jump. This fix a frustration
                // player have when they press jump on a bounce expecting to jump higher, but instead "use" their double
                // jump which cut air time leading to the inverse, a smaller jump.
                if (m_PlayerInput.Jump.WasPressedThisFrame() && !IsDoubleJumpPerformed && m_TimeSinceBounced > 0.1f)
                {
                    GameManager.Instance.AudioManager.PlaySFXAt(DoubleJumpAudio, transform.position);
                    IsDoubleJumpPerformed = true;
                    OnCharacterJumped?.Invoke(m_moveValue, groundNormal, currentPhysMaterial);
                    RigidbodyPlayer.linearVelocityY = 0; //double jump predictable from descending position by resetting the Y velocity
                    RigidbodyPlayer.AddForceY(jumpDoubleJumpForce, ForceMode2D.Impulse);
                    //Updates to animation controller
                    if (m_PlayerBehaviour.Animator != null)
                    {
                        m_PlayerBehaviour.Animator.SetTrigger("hasDoubleJumped");
                    }

                }

                var timeLapsedSincePress = Time.time - pressedJumpTime;
                if (m_PlayerInput.Jump.phase.IsInProgress() && !wasJumpButtonReleased)
                {
                    if (timeLapsedSincePress < timeLongPress && !IsCeiling)
                    {
                        //Debug.Log("in progress long jump");
                        RigidbodyPlayer.AddForceY(jumpForceLongPress * Time.deltaTime * m_TargetFPS * jumpLongPressAcceleration, ForceMode2D.Impulse);
                    }

                }
                //change gravity scale when falling after a threashold to remove the floatiness feeling
                if (RigidbodyPlayer.linearVelocityY < 2f)
                {
                    RigidbodyPlayer.gravityScale = gravityScaleWhenFallingDown;
                    /*
                //update the linear x when on air and about to landing
                onAirLinearX = m_rigidbodyPlayer.linearVelocityX;*/

                }
                else
                {
                    RigidbodyPlayer.gravityScale = m_GravityScaleNormal;
                }

            }
            
            if (m_PlayerInput.Jump.WasReleasedThisFrame())
            {
                wasJumpButtonReleased = true;
            }
            
            if (IsGrounded)
            {
                float groundAngleBoost = (1f + (1f - Vector2.Dot(Vector2.up, groundNormal))) * boostSteepGroundMultiplier; //boost of force as the inclination gets closer to the max angle of ground
                float forceMovementXdir = m_moveValue.x * accelerationXSpeed * groundAngleBoost; //* (Mathf.Abs(framesMovingSameDirection)*0.2f) * currentAirboneFriction;

                // Do we have user-input?
                if (Mathf.Abs(forceMovementXdir) > float.Epsilon)
                {
                    // Yes, so this always has the priority.
                    RigidbodyPlayer.AddForce(forceMovementXdir * -Vector2.Perpendicular(groundNormal), ForceMode2D.Force);
                }
                else
                {
                    // No, so we can use external forces.
                    // Here we're applying the conveyor impulse we're in contact with.
                    RigidbodyPlayer.AddForce(conveyorImpulse, ForceMode2D.Impulse);
                }
            
                // Reset any conveyor impulses.
                conveyorImpulse = Vector2.zero;
            }
            else
            {
                RigidbodyPlayer.AddForceX(m_moveValue.x * accelerationXSpeedOnAir); //* (Mathf.Abs(framesMovingSameDirection)*0.2f) * currentAirboneFriction;
                if (m_moveValue.x != 0)
                {
                    m_OnAirLinearX = RigidbodyPlayer.linearVelocityX;
                }
                else
                {
                    m_OnAirLinearX = 0;
                }
            
            }

            RigidbodyPlayer.linearVelocityX = Mathf.Clamp(RigidbodyPlayer.linearVelocityX, -m_CurrentXLimit, m_CurrentXLimit);
            RigidbodyPlayer.linearVelocityY = Mathf.Clamp(RigidbodyPlayer.linearVelocityY, -m_CurrentYLimit, m_CurrentYLimit);
            if (currentMovingPlatform != null)
            {
                /*
            Don't drive physics via the transform because it hides the fact that the two are not in-sync.
            The beauty of using velocity/move-position/position is that neither the body or the transform change happen there and then.
            They are updated when the physics runs or if you're doing fixed-update you also get interpolation working.
            You don't get interpolation if you're writing to the transform. It's just teleportation with no "movement".
            Physics wants to be in control of the transform. Unity does not have a graceful way of showing that.
            The differences between the transform and rigidbody pose might not be noticeable visually but I see a lot, devs who say "look, the character is touching on this frame but physics says it isn't".   This is nearly always caused by this discrepancy.
            */
                var deltaCurrentMovingPlatformMove = (Vector2)(currentMovingPlatform.transform.position - currentMovingPlatPrevFramePosition);
                RigidbodyPlayer.position += new Vector2(deltaCurrentMovingPlatformMove.x,deltaCurrentMovingPlatformMove.y);
                currentMovingPlatPrevFramePosition = currentMovingPlatform.transform.position;
            }
            #endregion
            #region Update Animation

            //Updates to camera and animation every n frames, otherwise animation can switch too fast and look jittery
            if (Time.frameCount % 10 == 0)
            {

                //Updates to animation controller
                if (m_PlayerBehaviour.Animator != null)
                {
                    m_PlayerBehaviour.Animator.SetFloat("linearVelocityX", MathF.Abs(RigidbodyPlayer.linearVelocityX));
                    m_PlayerBehaviour.Animator.SetFloat("linearVelocityY", RigidbodyPlayer.linearVelocityY);
                    m_PlayerBehaviour.Animator.SetFloat("moveValueX", IsScriptDriven ? 1.0f : m_moveValue.x);
                    m_PlayerBehaviour.Animator.SetBool("isGrounded", IsGrounded);
                }
                //cam switch based on velocity
                if (CloseUpCam != null)
                {
                    //when X or Y seed is close to the max speed limits
                    if (Mathf.Abs(RigidbodyPlayer.linearVelocityX) > m_CurrentXLimit * 0.5f || Mathf.Abs(RigidbodyPlayer.linearVelocityY) > m_CurrentYLimit * 0.8f)
                    {
                        CloseUpCam.Priority = -1; //depriotarize, far cam will come active
                    }
                    else
                    {
                        CloseUpCam.Priority = 1; //priotarize, idle cam will come active
                    }
                }
            }
            //to clean this code
            //inclination effect
            if (Mathf.Abs(m_moveValue.x) > 0.1f)
            {
                modelFacingDir = -Mathf.Sign(m_moveValue.x);
            }

            float groundAngle = modelHolder.localEulerAngles.z;
            if (groundAngle > 180f)
            {
                groundAngle -= 360f;
            }

            if (IsGrounded)
            {
                groundAngle = Vector2.SignedAngle(Vector2.up, groundNormal) * modelInclinationOnGround;
            }

            var facingRotation = Quaternion.AngleAxis(40f * modelFacingDir, Vector3.up); //40 is the angle to give the model a 3/4 angle
            var groundRotation = Quaternion.AngleAxis(groundAngle, Vector3.forward);
            var targetRotation = groundRotation * facingRotation;

            modelHolder.localRotation = Quaternion.Slerp(
                modelHolder.localRotation,
                targetRotation,
                1f - Mathf.Pow(rotationLerpFactor, Time.deltaTime)
            ); //smoothing would be needed
        }
        //private float _groundAngle = Quaternion.identity;
        #endregion
   
        #region Collision Enter
        void OnCollisionEnter2D(Collision2D collision)
        {
            var firstContact = collision.GetContact(0);
            float contactAngle = Vector2.Angle(Vector2.up, firstContact.normal);
            switch (collision.transform.tag)
        
            {
                case "Block":
                    if (contactAngle < 20f)
                    {
                        m_TimeSinceBounced = 0.0f;
                        RigidbodyPlayer.linearVelocityY = 0f;
                        RigidbodyPlayer.AddForceY(bounceForce, ForceMode2D.Impulse);
                        RigidbodyPlayer.linearVelocityX *= multiplierReduceLinearXafterBounce; //reduce the X velocity to have more control after the bounce
                        //to use for anim or others
                        if (m_PlayerBehaviour.Animator != null)
                        {
                            m_PlayerBehaviour.Animator.SetTrigger("hasBounced");
                        }
                    }
                    break;
                case "GroundMovingPlatform":
                    //to keep the character following the moving platform, active when landing on it from the top
                    if (contactAngle < 20f)
                    {
                        currentMovingPlatform = collision.gameObject;
                        currentMovingPlatPrevFramePosition = collision.gameObject.transform.position;
                    }
                    break;
                case "Ground":
                    //keep the on air velocity if moving, facing the same direction and the value is not 0
                    if (m_moveValue.x != 0 && Mathf.Sign(m_OnAirLinearX) == Mathf.Sign(m_moveValue.x) && m_OnAirLinearX != 0)
                    {
                        RigidbodyPlayer.linearVelocityX = m_OnAirLinearX; //keep on air momentum
                        m_OnAirLinearX = 0;
                    
                    } 
                    break;
                case "Spikes":
                    //send damage to the player behaviour
                    if (TryGetComponent<PlayerBehaviour>(out PlayerBehaviour pb))
                    {
                        SpikeContact(firstContact, pb);
                    }
                    break;
                case "Enemy":
                    //to use if there was an enemy that collider with rigibody
                    break;
                case "Untagged":
                    break;
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            var firstContact = collision.GetContact(0);
            float contactAngle = Vector2.Angle(Vector2.up, firstContact.normal);
            switch (collision.transform.tag)
            {
                case "Spikes":
                    //send damage to the player behaviour
                    if (TryGetComponent<PlayerBehaviour>(out PlayerBehaviour pb))
                    {
                        SpikeContact(firstContact, pb);
                    }
                    break;
            }
        }

        #endregion
        #region Collision Exit
        void OnCollisionExit2D(Collision2D collision)
        {
            switch (collision.transform.tag)
        
            {
                case "Block":
                
                    break;
                case "GroundMovingPlatform":
                
                    currentMovingPlatform = null;
                    break;
                case "Ground":
                
                    break;
                case "Spikes":
                
                    break;
                case "Enemy":
                
                    break;
                case "Spring":
                
                    break;

                case "Untagged":
                    //Debug.LogError("Untagged object exit colliding");
                    break;
            }
        }
        #endregion
        
        void OnEnable()
        {
            m_PlayerInput = GetComponent<PlayerInput>();
            m_PlayerBehaviour = GetComponent<PlayerBehaviour>();
            RigidbodyPlayer = GetComponent<Rigidbody2D>();

            if (m_PlayerBehaviour != null)
                m_PlayerBehaviour.OnMaskingChanged += OnMaskingChanged;
        }
        void OnDisable()
        {
            ResetMovement();

            if (m_PlayerBehaviour != null)
                m_PlayerBehaviour.OnMaskingChanged -= OnMaskingChanged;
        }

        void OnMaskingChanged(bool masked)
        {
            if (masked)
            {
                m_CurrentXLimit = PortalXSpeedLimit;
                m_CurrentYLimit = PortalYSpeedLimit;
            }
            else
            {
                m_CurrentXLimit = XspeedLimit;
                m_CurrentYLimit = YspeedLimit;
            }
        }

        void SpikeContact(ContactPoint2D contactPoint, PlayerBehaviour playerBehaviour)
        {
            //compute a point inside the tile that created the contact so we can sample it
            var tilePosition = contactPoint.point - contactPoint.normal * 0.1f;
            //TODO: store somewhere to not request each time?
            var tilemap = contactPoint.collider.GetComponent<Tilemap>();
            var tileTransform = tilemap.GetTransformMatrix(tilemap.WorldToCell(tilePosition));
           
            Vector3 spikeDirection = tileTransform * Vector3.up;
            float dot = Vector3.Dot(spikeDirection, contactPoint.normal);
            
            if(dot > 0.9f)
                playerBehaviour.ReceiveDamage(1,contactPoint.point);
        }

        //call if the player need to be "stopped", e.g. when the player respawn, need to kill its velocity
        public void ResetMovement()
        {
            RigidbodyPlayer.linearVelocity = Vector2.zero;
        }

        public void Bounced()
        {
            m_TimeSinceBounced = 0.0f;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (IsDrawingDebugInfo && EditorApplication.isPlaying)
            {
                Handles.Label(infoContactPoint + (infoContactPoint - (Vector2)transform.position) * 0.5f, infoContactPointDegrees.ToString());
                Handles.Label(infoContactPoint + (infoContactPoint - (Vector2)transform.position) * 1f, groundNormal.ToString());
                if (IsGrounded)
                {
                    if (IsSliding)
                    {
                        Handles.color = Color.red;
                    }
                    else
                    {
                        Handles.color = Color.blue;
                    }

                    Handles.DrawSolidDisc(transform.position, Vector3.forward, 0.5f);
                }
            }
        }
#endif
    }
}