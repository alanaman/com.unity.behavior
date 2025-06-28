using System.Collections.Generic;
using System.Linq;
using Unity.Behavior.GraphFramework;
using UnityEngine;

#if NETCODE_FOR_GAMEOBJECTS
using Unity.Netcode;
#endif

namespace Unity.Behavior
{
    /// <summary>
    /// <para>Manages a behavior graph's lifecycle on a GameObject and handles data through blackboard variables.</para>
    /// <para>The BehaviorGraphAgent maintains the following lifecycle states:</para>
    /// <para>- <b>Uninitialized</b> - The graph has been assigned but not instantiated yet</para>
    /// <para>- <b>Initialized</b> - The graph has been instantiated with a unique copy for this agent</para>
    /// <para>- <b>Started</b> - The graph has started running</para>
    /// <para>- <b>Running</b> - The graph is being updated each frame via Tick()</para>
    /// <para>- <b>Ended</b> - The graph has been stopped and is no longer running</para>
    ///
    /// <para><b>Initialization Sequence:</b></para>
    /// <para>- When a graph is assigned in the Inspector, it's automatically initialized during Awake()</para>
    /// <para>- When assigning a graph via the Graph property at runtime, it's automatically initialized during the next Update()</para>
    /// <para>- You can also explicitly control initialization by calling Init() manually</para>
    ///
    /// <para><b>Blackboard Variable Handling:</b></para>
    /// <para>- Before initialization: SetVariableValue() sets agent-level overrides (visible in the Inspector)</para>
    /// <para>- After initialization: SetVariableValue() sets values in the instanced graph's blackboard</para>
    /// </summary>
    /// <example>
    /// <para><b>Common Usage Patterns:</b></para>
    /// <code>
    /// // Basic usage - assign graph and configure at runtime
    /// agent.Graph = myBehaviorGraph;  // Graph will auto-initialize next Update
    /// agent.SetVariableValue("Destination", targetPosition);
    /// 
    /// // Template pattern - configure, then instantiate multiple agents
    /// templateAgent.Graph = sharedGraph;
    /// templateAgent.SetVariableValue("Speed", defaultSpeed);  // Sets override
    /// 
    /// var newAgent = Instantiate(templateAgent);
    /// newAgent.Init();  // Explicitly initialize
    /// newAgent.SetVariableValue("PatrolPoints", uniquePatrolPoints);  // Per-instance value
    /// </code>
    /// </example>
    [AddComponentMenu("AI/Behavior Container")]
    public class BehaviorGraphContainer : BehaviorGraphAgent
    {
        /// <summary>
        /// <para>The graph of behaviours to be executed by the agent.</para>
        /// <para><b>When assigning a new graph to this property:</b></para>
        /// <para>- The agent will be marked as uninitialized and will automatically initialize during the next Update cycle (if agent is enabled)</para>
        /// <para>- You don't have to manually call Init() or Start() when setting this property</para>
        ///
        /// <para><b>About blackboard variable:</b></para>
        /// <para>- Calling SetVariableValue() before the agent is initialized (after setting Graph but before the next Update) 
        /// will set blackboard overrides at the agent level, visible in the inspector</para>
        /// <para>- Calling SetVariableValue() after the agent is initialized will modify the individual instance variables</para>
        /// <para>- This makes it possible to set default values that apply to all instances, or customize individual agent behaviors</para>
        /// </summary>
        /// <example>
        /// <code>
        /// // Assign graph and set default value before initialization
        /// agent.Graph = myGraph;
        /// agent.SetVariableValue("Destination", new Vector3(10, 0, 10)); // Sets agent-level override
        /// 
        /// // After automatic initialization in Update, or manual Init():
        /// agent.SetVariableValue("PatrolPoints", customPatrolPoints); // Sets instance-specific value
        /// </code>
        /// </example>
        protected override void Awake() { }
        public override void Update() { }
        public override void Start() { }
        public override void End() { }


        public void InitGraph()
        {
            Init();
        }
        
        /// <summary>
        /// Begins execution of the agent's behavior graph.
        /// </summary>
        public void StartGraph()
        {
            if (m_Graph == null) return;
#if NETCODE_FOR_GAMEOBJECTS
            if (!IsOwner && NetcodeRunOnlyOnOwner) return;
#endif

            if (!isActiveAndEnabled)
            {
                if (!m_IsInitialised)
                {
                    return;
                }

                if (m_Graph.IsRunning)
                {
                    return;
                }
                m_Graph.End();
                m_IsStarted = false;
                return;
            }

            if (!m_IsInitialised)
            {
                Init();
                // If the graph was invalid, it would be cleared by now.
                if (m_Graph == null)
                {
                    return;
                }
            }
            if (m_Graph.IsRunning)
            {
                return;
            }
            m_Graph.Start();
            m_IsStarted = true;
        }

        /// <summary>
        /// Ends the execution of the agent's behavior graph.
        /// </summary>
        public void EndGraph()
        {
            if (m_Graph == null || m_Graph.RootGraph == null) return;
#if NETCODE_FOR_GAMEOBJECTS
            if (!IsOwner && NetcodeRunOnlyOnOwner) return;
#endif
            m_Graph.End();
        }

        /// <summary>
        /// Restarts the execution of the agent's behavior graph.
        /// </summary>
        public void RestartGraph()
        {
#if NETCODE_FOR_GAMEOBJECTS
            if (!IsOwner && NetcodeRunOnlyOnOwner) return;
#endif
            if (m_Graph == null)
            {
                Debug.LogError("Can't restart the agent because no graph has been assigned.", this);
                return;
            }

            if (!isActiveAndEnabled)
            {
                if (m_IsInitialised)
                {
                    m_Graph.End();
                }
                m_IsStarted = false;
                return;
            }

            if (!m_IsInitialised)
            {
                // The graph needs initialising and then starting. The user asked to do it this frame so we do it here
                // instead of waiting for Update().
                Init();
                // If the graph was invalid, it would be cleared by now.
                if (m_Graph == null)
                {
                    return;
                }
                m_Graph.Start();
                m_IsStarted = true;
                return;
            }
            m_Graph.Restart();
            m_IsStarted = true;
        }

        /// <summary>
        /// Ticks the agent's behavior graph and initializes and starts the graph if necessary.
        /// </summary>
        public void UpdateGraph()
        {
            if (m_Graph == null || m_Graph.RootGraph == null)
                return;

#if NETCODE_FOR_GAMEOBJECTS
            if (!IsOwner && NetcodeRunOnlyOnOwner) return;
#endif
            
            if (!m_IsInitialised)
            {
                Init();
            }

            if (!m_IsStarted)
            {
                m_Graph.Start();
                m_IsStarted = true;
            }
            m_Graph.Tick();
        }

#if NETCODE_FOR_GAMEOBJECTS
        public override void OnDestroy()
        {
            base.OnDestroy();
#else
        private void OnDestroy()
        {
#endif
            if (m_Graph)
            {
                m_Graph.End();
            }
        }
    }
}