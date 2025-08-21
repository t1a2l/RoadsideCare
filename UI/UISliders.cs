using ColossalFramework.UI;
using UnityEngine;

namespace RoadsideCare.UI
{
    public static class UISliders
    {
        public static UISlider AddPlainSlider(UIComponent parent, float xPos, float yPos, string text, float min, float max, float step, float defaultValue, float width = 600f)
        {
            // Add slider component.
            UIPanel sliderPanel = parent.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsSliderTemplate")) as UIPanel;

            // Panel layout.
            sliderPanel.autoLayout = false;
            sliderPanel.autoSize = false;
            sliderPanel.width = width + 50f;
            sliderPanel.height = 65f;

            // Label.
            UILabel sliderLabel = sliderPanel.Find<UILabel>("Label");
            sliderLabel.autoHeight = true;
            sliderLabel.width = width;
            sliderLabel.anchor = UIAnchorStyle.Left | UIAnchorStyle.Top;
            sliderLabel.relativePosition = Vector3.zero;
            sliderLabel.text = text;

            // Slider configuration.
            UISlider newSlider = sliderPanel.Find<UISlider>("Slider");
            newSlider.minValue = min;
            newSlider.maxValue = max;
            newSlider.stepSize = step;
            newSlider.value = defaultValue;

            // Move default slider position to match resized label.
            newSlider.anchor = UIAnchorStyle.Left | UIAnchorStyle.Top;
            newSlider.relativePosition = UILayout.PositionUnder(sliderLabel);

            newSlider.width = width;

            // Set position.
            newSlider.parent.relativePosition = new Vector2(xPos, yPos);

            return newSlider;
        }

        public static UISlider AddPlainSliderWithValue(UIComponent parent, float xPos, float yPos, string text, float min, float max, float step, float defaultValue, SliderValueFormat format, float width = 600f)
        {
            // Add slider component.
            UISlider newSlider = AddPlainSlider(parent, xPos, yPos, text, min, max, step, defaultValue, width);
            UIPanel sliderPanel = (UIPanel)newSlider.parent;

            // Value label.
            UILabel valueLabel = sliderPanel.AddUIComponent<UILabel>();
            valueLabel.name = "ValueLabel";
            valueLabel.relativePosition = UILayout.PositionRightOf(newSlider, 8f, 1f);

            // Set initial value and event handler to update on value change.
            valueLabel.text = format.FormatValue(newSlider.value);
            newSlider.eventValueChanged += (c, value) => valueLabel.text = format.FormatValue(value);

            return newSlider;
        }

        public static UISlider AddPlainSliderWithValue(UIComponent parent, float xPos, float yPos, string text, float min, float max, float step, float defaultValue, float width = 600f) =>
            AddPlainSliderWithValue(parent, xPos, yPos, text, min, max, step, defaultValue, new SliderValueFormat(valueMultiplier: 1, roundToNearest: step, numberFormat: "N", suffix: null), width);

        /// <summary>
        /// Provides formatting for slider value displays.
        /// </summary>
        public readonly struct SliderValueFormat(float valueMultiplier = 1f, float roundToNearest = 1f, string numberFormat = "N", string suffix = null)
        {
            // Formatting options.
            private readonly float _valueMultiplier = valueMultiplier;
            private readonly float _roundToNearest = roundToNearest;
            private readonly string _numberFormat = numberFormat;
            private readonly string _valueSuffix = suffix;

            public string FormatValue(float value)
            {
                string valueText = (value * _valueMultiplier).RoundToNearest(_roundToNearest).ToString(_numberFormat);
                if (!string.IsNullOrEmpty(_valueSuffix))
                {
                    valueText += _valueSuffix;
                }

                return valueText;
            }
        }

    }
}
