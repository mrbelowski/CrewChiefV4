using CrewChiefV4.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV4.Events
{
    public class CornerData {
        
        public Dictionary<Enum, Corners> cornersForEachStatus = new Dictionary<Enum, Corners>();

        public class EnumWithThresholds
        {
            public Enum e;
            public float lowerThreshold;
            public float upperThreshold;
            public EnumWithThresholds(Enum e, float lowerThreshold, float upperThreshold)
            {
                this.e = e;
                this.lowerThreshold = lowerThreshold;
                this.upperThreshold = upperThreshold;
            }
        }

        public enum Corners
        {
            FRONT_LEFT, FRONT_RIGHT, REAR_LEFT, REAR_RIGHT, FRONTS, REARS, LEFTS, RIGHTS, ALL, NONE
        }

        public Corners getCornersForStatus(Enum status)
        {
            Corners cornerStatus = Corners.NONE;
            if (cornersForEachStatus.TryGetValue(status, out cornerStatus))
            {
                return cornerStatus;
            }
            else
            {
                return Corners.NONE;
            }
        }

        public Boolean hasValueAtLevel(Enum e)
        {
            Corners cornerStatus = Corners.NONE;
            if (cornersForEachStatus.TryGetValue(e, out cornerStatus))
            {
                return cornerStatus != Corners.NONE;
            }

            return false;
        }

        private static void addCornerStatus(CornerData cornerData, Enum e, Corners corners) 
        {
            if (cornerData.cornersForEachStatus.ContainsValue(corners))
            {
                var item = cornerData.cornersForEachStatus.First(kvp => kvp.Value == corners);
                cornerData.cornersForEachStatus.Remove(item.Key);
            }
            cornerData.cornersForEachStatus.Add(e, corners);
        }

        public static CornerData getCornerData(List<EnumWithThresholds> enumsWithThresholds, float leftFrontValue, float rightFrontValue, float leftRearValue, float rightRearValue)
        {
            CornerData cornerData = new CornerData();
            foreach (EnumWithThresholds enumWithThresholds in enumsWithThresholds)
            {
                if (leftFrontValue >= enumWithThresholds.lowerThreshold && leftFrontValue < enumWithThresholds.upperThreshold)
                {
                    if (rightFrontValue >= enumWithThresholds.lowerThreshold && rightFrontValue < enumWithThresholds.upperThreshold)
                    {
                        if (leftRearValue >= enumWithThresholds.lowerThreshold && leftRearValue < enumWithThresholds.upperThreshold &&
                            rightRearValue >= enumWithThresholds.lowerThreshold && rightRearValue < enumWithThresholds.upperThreshold)
                        {
                            // it's 'whatever' all around
                            addCornerStatus(cornerData, enumWithThresholds.e, Corners.ALL);
                        }
                        else
                        {
                            // front sides
                            addCornerStatus(cornerData, enumWithThresholds.e, Corners.FRONTS);
                        }
                    }
                    else if (leftRearValue >= enumWithThresholds.lowerThreshold && leftRearValue < enumWithThresholds.upperThreshold)
                    {
                        addCornerStatus(cornerData, enumWithThresholds.e, Corners.LEFTS);
                    }
                    else
                    {
                        addCornerStatus(cornerData, enumWithThresholds.e, Corners.FRONT_LEFT);
                    }
                }
                else if (rightFrontValue >= enumWithThresholds.lowerThreshold && rightFrontValue < enumWithThresholds.upperThreshold)
                {
                    if (rightRearValue >= enumWithThresholds.lowerThreshold && rightRearValue < enumWithThresholds.upperThreshold)
                    {
                        addCornerStatus(cornerData, enumWithThresholds.e, Corners.RIGHTS);
                    }
                    else
                    {
                        addCornerStatus(cornerData, enumWithThresholds.e, Corners.FRONT_RIGHT);
                    }
                }
                else if (leftRearValue >= enumWithThresholds.lowerThreshold && leftRearValue < enumWithThresholds.upperThreshold)
                {
                    if (rightRearValue >= enumWithThresholds.lowerThreshold && rightRearValue < enumWithThresholds.upperThreshold)
                    {
                        addCornerStatus(cornerData, enumWithThresholds.e, Corners.REARS);
                    }
                    else
                    {
                        addCornerStatus(cornerData, enumWithThresholds.e, Corners.REAR_LEFT);
                    }
                }
                else if (rightRearValue >= enumWithThresholds.lowerThreshold && rightRearValue < enumWithThresholds.upperThreshold)
                {
                    addCornerStatus(cornerData, enumWithThresholds.e, Corners.REAR_RIGHT);
                }
            }
            return cornerData;
        }
    }
}
