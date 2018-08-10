#pragma once

#include <vector>
#include <Fusion/Components/Joint.h>
#include <Fusion/Components/Occurrence.h>
#include "XmlWriter.h"
#include "Driver.h"
#include "JointSensor.h"
#include "../Vector3.h"

using namespace adsk;

namespace BXDJ
{	
	class RigidNode;
	class ConfigData;

	///
	/// Serves as an abstract class for objects that connect two RigidNodes together with a defined type of motion.
	///
	class Joint : public XmlWritable
	{
	public:
		/// Copy constructor.
		Joint(const Joint &);

		///
		/// Creates a joint between an existing RigidNode and one that will be created.
		/// \param parent RigidNode that will serve as the parent of the joint.
		/// \param fusionJoint The Fusion joint to base the new Joint off of.
		/// \param parentOccurrence The Fusion occurrence in the Fusion joint that is owned by the parent RigidNode.
		///                         The other occurrence in the Fusion joint will become the primary occurrence for the new child RigidNode.
		///
		Joint(RigidNode *, core::Ptr<fusion::Joint>, core::Ptr<fusion::Occurrence>);

		RigidNode * getParent() const; ///< \return The parent RigidNode of the Joint.
		std::shared_ptr<RigidNode> getChild() const; ///< \return The child RigidNode of the Joint.
		Vector3<> getParentBasePoint() const; ///< \return The point in space at which the parent occurrence is connected to the child occurrence in Fusion.
		Vector3<> getChildBasePoint() const; ///< \return The point in space at which the child occurrence is connected to the parent occurrence in Fusion.

		///
		/// Searches a collection of ConfigData for the driver assigned to this Joint, then copies said Driver onto this Joint.
		/// \param config ConfigData to search.
		///
		virtual void applyConfig(const ConfigData & config) = 0;
		void setDriver(Driver); ///< Applies a Driver to this Joint.
		void setNoDriver(); ///< Removes any Driver from this Joint.
		std::unique_ptr<Driver> getDriver() const; ///< \return A copy of any Driver applied to this Joint. Returns nullptr the Joint has no Driver.

		void addSensor(JointSensor); ///< Adds a JointSensor to this Joint.
		void clearSensors(); ///< Removes all JointSensors from this Joint.

	protected:
		/// Used for specifying which occurrence in a Joint is recognized as the parent.
		enum OneTwo : bool { ONE = true /**< joint->occurrenceOne() */, TWO = false /**< joint->occurrenceTwo() */ };

		core::Ptr<fusion::Joint> getFusionJoint() { return fusionJoint; } ///< \return The joint in Fusion that was used to create this Joint.
		OneTwo getParentOccNum() { return parentOcc; } ///< \return Which Fusion occurrence (One or Two) that the parent of this Joint is in the Fusion joint.
		virtual void write(XmlWriter &) const; ///< This should be called by any derived Joint classes. Writes driver and sensors to the BXDJ file.

	private:
		OneTwo parentOcc; ///< Specifies which occurrence in the Fusion joint is recognized as the parent.
		core::Ptr<fusion::Joint> fusionJoint; ///< The Fusion joint used to create this Joint.
		RigidNode * parent; ///< The RigidNode that this Joint is a child of.
		std::shared_ptr<RigidNode> child; ///< The RigidNode that is a child of this Joint.
		std::unique_ptr<Driver> driver; ///< The Driver applied to this Joint.

		std::vector<std::shared_ptr<JointSensor>> sensors; ///< Contains any JointSensors attached to this Joint.

	};
};
