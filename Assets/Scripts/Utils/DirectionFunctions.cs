using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    // Funciones recurrentes para el trato con direcciones
    public static class DirectionFunctions
    {
        public static List<Directions> AllDirections { get => new()
            {
                Directions.TOP,
                Directions.BOT,
                Directions.RIGHT,
                Directions.LEFT
            };
        }

        public static Directions GetOppositeDirection(Directions _direction)
        {
            switch (_direction)
            {
                case Directions.TOP:
                    return Directions.BOT;
                case Directions.BOT:
                    return Directions.TOP;
                case Directions.RIGHT:
                    return Directions.LEFT;
                case Directions.LEFT:
                    return Directions.RIGHT;
                default:
                    return Directions.NULL;
            }
        }

        public static Vector3 GetSumDirection(Directions _direction, float _sumValue)
        {
            switch (_direction)
            {
                case Directions.TOP:
                    return new(0.0f, 0.0f, _sumValue);
                case Directions.BOT:
                    return new(0.0f, 0.0f, -_sumValue);
                case Directions.RIGHT:
                    return new(_sumValue, 0.0f, 0.0f);
                case Directions.LEFT:
                    return new(-_sumValue, 0.0f, 0.0f);
                default:
                    return Vector3.zero;
            }
        }
    }
}