using UnityEngine;
using Unity.MLAgents.Sensors;

namespace Unity.MLAgents.Extensions.Sensors
{
    /// <summary>
    /// ISensor implementation that generates observations for a group of Rigidbodies or ArticulationBodies.
    /// </summary>
    public class PhysicsBodySensor : ISensor
    {
        int[] m_Shape;
        string m_SensorName;

        PoseExtractor m_PoseExtractor;
        IJointExtractor[] m_JointExtractors;
        PhysicsSensorSettings m_Settings;

        /// <summary>
        ///  Construct a new PhysicsBodySensor
        /// </summary>
        /// <param name="rootBody"></param>
        /// <param name="settings"></param>
        /// <param name="sensorName"></param>
        public PhysicsBodySensor(Rigidbody rootBody, GameObject rootGameObject, PhysicsSensorSettings settings, string sensorName=null)
        {
            m_PoseExtractor = new RigidBodyPoseExtractor(rootBody, rootGameObject);
            m_SensorName = string.IsNullOrEmpty(sensorName) ? $"PhysicsBodySensor:{rootBody?.name}" : sensorName;
            m_Settings = settings;
            m_JointExtractors = new IJointExtractor[0];

            var numTransformObservations = settings.TransformSize(m_PoseExtractor.NumPoses);
            // TODO Account for IJointExtractor sizes
            m_Shape = new[] { numTransformObservations };
        }

#if UNITY_2020_1_OR_NEWER
        public PhysicsBodySensor(ArticulationBody rootBody, PhysicsSensorSettings settings, string sensorName=null)
        {
            var poseExtractor = new ArticulationBodyPoseExtractor(rootBody);
            m_PoseExtractor = poseExtractor;
            m_SensorName = string.IsNullOrEmpty(sensorName) ? $"ArticulationBodySensor:{rootBody?.name}" : sensorName;
            m_Settings = settings;

            var numJointExtractorObservations = 0;
            var articBodies = poseExtractor.Bodies;
            m_JointExtractors = new IJointExtractor[articBodies.Length - 1]; // skip the root
            for (var i = 1; i < articBodies.Length; i++)
            {
                var jointExtractor= new ArticulationBodyJointExtractor(articBodies[i]);
                numJointExtractorObservations += jointExtractor.NumObservations(settings);
                m_JointExtractors[i - 1] = jointExtractor;
            }

            var numTransformObservations = settings.TransformSize(m_PoseExtractor.NumPoses);
            m_Shape = new[] { numTransformObservations + numJointExtractorObservations };
        }
#endif

        /// <inheritdoc/>
        public int[] GetObservationShape()
        {
            return m_Shape;
        }

        /// <inheritdoc/>
        public int Write(ObservationWriter writer)
        {
            var numWritten = writer.WritePoses(m_Settings, m_PoseExtractor);
            foreach (var jointExtractor in m_JointExtractors)
            {
                numWritten += jointExtractor.Write(m_Settings, writer, numWritten);
            }
            return numWritten;
        }

        /// <inheritdoc/>
        public byte[] GetCompressedObservation()
        {
            return null;
        }

        /// <inheritdoc/>
        public void Update()
        {
            if (m_Settings.UseModelSpace)
            {
                m_PoseExtractor.UpdateModelSpacePoses();
            }

            if (m_Settings.UseLocalSpace)
            {
                m_PoseExtractor.UpdateLocalSpacePoses();
            }
        }

        /// <inheritdoc/>
        public void Reset() {}

        /// <inheritdoc/>
        public SensorCompressionType GetCompressionType()
        {
            return SensorCompressionType.None;
        }

        /// <inheritdoc/>
        public string GetName()
        {
            return m_SensorName;
        }
    }
}
