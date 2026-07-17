using System;
using GraphWarRemake.Logging;
using NCalc;
using UnityEngine;

namespace GraphWarRemake.Math
{
    /// <summary>
    /// Движок для вычисления траекторий снарядов на основе математических формул.
    /// Регистрируется как Singleton в VContainer.
    /// Вычисления строго на стороне сервера (Хоста).
    /// </summary>
    public class MathEngine
    {
        private readonly IGameLogger _logger;

        public MathEngine(IGameLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Вычисляет следующую позицию снаряда по формуле.
        /// Формула — строка вида "sin(t)", "0.01*t^2 + 5", "cos(t*0.5)*10" и т.д.
        /// Поддерживаемые операции: +, -, *, /, ^ (степень), () ,
        /// функции: sin, cos, tan, sqrt, abs, log, ln, pi, e.
        /// </summary>
        /// <param name="formula">Математическая формула (переменная — t).</param>
        /// <param name="t">Текущий параметр времени (ось X /.progress полёта).</param>
        /// <param name="startPos">Начальная позиция снаряда.</param>
        /// <returns>Вычисленная позиция в пространстве.</returns>
        public Vector3 CalculateNextPosition(string formula, float t, Vector3 startPos)
        {
            _logger.Log($"Вычисление позиции: формула=\"{formula}\", t={t:F2}, start={startPos}");

            float x = startPos.x + t;
            float y = EvaluateFormula(formula, t);
            float z = startPos.z;

            var result = new Vector3(x, y, z);

            _logger.Log($"Результат: ({result.x:F2}, {result.y:F2}, {result.z:F2})");

            return result;
        }

        /// <summary>
        /// Вычисляет значение формулы для заданного t.
        /// Использует NCalc для безопасного вычисления выражений.
        /// </summary>
        private float EvaluateFormula(string formula, float t)
        {
            try
            {
                var expression = new Expression(formula);

                // Передаём переменную t в выражение
                expression.Parameters["t"] = (double)t;

                // Регистрируем пользовательские функции (API NCalc 6.x)
                expression.Functions["SIN"] = args => System.Math.Sin(Convert.ToDouble(args.Evaluate(0)));
                expression.Functions["COS"] = args => System.Math.Cos(Convert.ToDouble(args.Evaluate(0)));
                expression.Functions["TAN"] = args => System.Math.Tan(Convert.ToDouble(args.Evaluate(0)));
                expression.Functions["SQRT"] = args => System.Math.Sqrt(Convert.ToDouble(args.Evaluate(0)));
                expression.Functions["ABS"] = args => System.Math.Abs(Convert.ToDouble(args.Evaluate(0)));
                expression.Functions["LOG"] = args => System.Math.Log10(Convert.ToDouble(args.Evaluate(0)));
                expression.Functions["LN"] = args => System.Math.Log(Convert.ToDouble(args.Evaluate(0)));
                expression.Functions["POW"] = args => System.Math.Pow(Convert.ToDouble(args.Evaluate(0)), Convert.ToDouble(args.Evaluate(1)));
                expression.Functions["CLAMP"] = args => System.Math.Clamp(Convert.ToDouble(args.Evaluate(0)), Convert.ToDouble(args.Evaluate(1)), Convert.ToDouble(args.Evaluate(2)));
                expression.Functions["MIN"] = args => System.Math.Min(Convert.ToDouble(args.Evaluate(0)), Convert.ToDouble(args.Evaluate(1)));
                expression.Functions["MAX"] = args => System.Math.Max(Convert.ToDouble(args.Evaluate(0)), Convert.ToDouble(args.Evaluate(1)));

                var result = expression.Evaluate();

                if (result is double doubleResult)
                    return (float)doubleResult;

                if (result is int intResult)
                    return intResult;

                _logger.LogWarning($"NCalc вернул неожиданный тип: {result?.GetType()}");
                return 0f;
            }
            catch (Exception e)
            {
                _logger.LogError($"Ошибка вычисления формулы \"{formula}\": {e.Message}");
                return 0f;
            }
        }


    }
}
