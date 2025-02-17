using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Naninovel;
using TMPro;

namespace NaninovelSceneAssistant
{
	public class SceneAssistantMenu : SceneAssistantWindowMenu, ISceneAssistantLayout
	{
		[Header("Main elements")]
		[SerializeField] private CanvasGroup mainWindow;
		[SerializeField] private Transform parameterContainer;
		[SerializeField] private TMP_InputField copyBufferField;
		[SerializeField] private TextMeshProUGUI saveInfoBox;
		[SerializeField] private Button saveButton;
		[SerializeField] private TMP_InputField commandNameField;

		[Header("Object type section")]
		[SerializeField] private RectTransform objectTypeToggleContainer;
		[SerializeField] private ObjectTypeToggle objectTypeTogglePrototype;

		[Header("Id section")]
		[SerializeField] private Button idButton;
		[SerializeField] private TMP_Dropdown idDropdown;

		[Header("Field prototypes")]
		[SerializeField] private SliderField sliderFieldPrototype;
		[SerializeField] private InputField inputFieldPrototype;
		[SerializeField] private DropdownField dropdownFieldPrototype;
		[SerializeField] private ToggleField toggleFieldPrototype;
		[SerializeField] private ScrollableInputField scrollableFieldPrototype;
		[SerializeField] private ColorField colorFieldPrototype;
		[SerializeField] private ListField listFieldPrototype;

		public List<ISceneAssistantUIField> DataFields { get => parameterContainer.GetComponentsInChildren<ISceneAssistantUIField>().ToList(); }
		public List<ObjectTypeToggle> ObjectTypeToggles { get => objectTypeToggleContainer.GetComponentsInChildren<ObjectTypeToggle>().ToList(); }
		public INaninovelObjectData CurrentObject { get; protected set; }
		
		private static int lastIndex;
		private static string lastObject;
		
		public TMP_InputField CopyBufferField => copyBufferField;
		
		private Transform targetContainer;
		const string sceneAssistantDirectory =  "SceneAssistant";
		const string sceneAssistantFileName = "SceneAssistant.nani";

		protected override void Awake()
		{
			base.Awake();
			#if UNITY_WEBGL && !UNITY_EDITOR
			saveButton.gameObject.SetActive(false);
			commandNameField.gameObject.SetActive(false);
			saveInfoBox.gameObject.SetActive(false);
			#endif
		}

		public override void InitializeMenu()
		{
			base.InitializeMenu();
			idDropdown.onValueChanged.AddListener(DisplayObjectParameters);
			idButton.onClick.AddListener(CopyIdString);
			saveButton.onClick.AddListener(SaveCommandStringOnClick);
			commandNameField.onSubmit.AddListener(SaveCommandString);
		}
		
		protected override void OnDisable()
		{
			base.OnDisable();
			idDropdown.onValueChanged.RemoveListener(DisplayObjectParameters);
			idButton.onClick.RemoveListener(CopyIdString);
			saveButton.onClick.RemoveListener(SaveCommandStringOnClick);
			commandNameField.onSubmit.RemoveListener(SaveCommandString);
		}
		
		protected override void ClearMenu()
		{
			if(CurrentObject != null)
			{
				lastIndex = idDropdown.value;
				lastObject = idDropdown.options.ElementAt(lastIndex).text;
			}
			
			idDropdown.ClearOptions();
			ClearParameterFields();
			ClearToggles();
			saveInfoBox.text = String.Empty;
		}
		
		protected override void ResetMenu()
		{
			idDropdown.AddOptions(Manager.ObjectList.Keys.Select(v => new TMP_Dropdown.OptionData(v)).ToList());
			ResetToggles();
			
			if(!string.IsNullOrEmpty(lastObject))
			{
				if(Manager.ObjectList.Keys.ElementAt(lastIndex) == lastObject) 
				{
					idDropdown.value = lastIndex;
					DisplayObjectParameters(lastIndex);
					return;
				}
				else if(idDropdown.options.Any(o => o.text == lastObject))
				{
					var newIndex = idDropdown.options.FindIndex(o => o.text == lastObject);
					idDropdown.value = newIndex;
					DisplayObjectParameters(newIndex);
					return;
				}
			}

			idDropdown.value = 0;
			DisplayObjectParameters(0);
		}

		private void ResetToggles()
		{
			foreach (var kv in Manager.ObjectTypeList)
			{
				var toggle = Instantiate(objectTypeTogglePrototype, objectTypeToggleContainer);
				toggle.Initialize(kv);
			}
		}

		protected void DisplayObjectParameters(int index)
		{
			ClearParameterFields();	
			CurrentObject = Manager.ObjectList.ElementAt(index).Value;
			GenerateLayout(CurrentObject.CommandParameters, parameterContainer);
			saveInfoBox.text = String.Empty;
		}

		protected void ClearParameterFields()
		{
			foreach (var field in DataFields) Destroy(field.GameObject);
		}
		
		protected void ClearToggles()
		{
			foreach (var toggle in ObjectTypeToggles) Destroy(toggle.gameObject);
		}

		private void CopyIdString() => CopyToBuffer(CurrentObject.Id);

		public void CopyToBuffer(string text)
		{
			GUIUtility.systemCopyBuffer = text;
			copyBufferField.text = text;
			saveInfoBox.text = String.Empty;
		}

		public void GenerateLayout(List<ICommandParameterData> list, Transform parent)
		{
			targetContainer = parent;
			foreach (var data in list) data.DrawLayout(this);
		}
		
		public void UpdateDataValues() => DataFields.ForEach(f => f.GetDataValue());
		
		private string GetDataPath()
		{
			#if UNITY_ANDROID 
			return Application.persistentDataPath;
			#else 
			return Application.streamingAssetsPath;
			#endif
		}
		
		private void SaveCommandStringOnClick() => SaveCommandString(commandNameField.text); 

		private void SaveCommandString(string value)
		{
			var directoryPath = $"{GetDataPath()}/{sceneAssistantDirectory}";
			var filePath = $"{directoryPath}/{sceneAssistantFileName}";
			
			if(String.IsNullOrEmpty(copyBufferField.text))
			{
				saveInfoBox.color = Color.red;
				saveInfoBox.text = $"String is empty";
				return;
			}
			
			if(!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

			if(!File.Exists(filePath)) File.WriteAllText(filePath, GetGeneratedString());
			else File.AppendAllText(filePath, GetGeneratedString());
			
			saveInfoBox.color = Color.green;
			saveInfoBox.text = $"Successfully saved string to <b>{sceneAssistantFileName}<b> at {GetDataPath()}";
		}

		private string GetGeneratedString() => !String.IsNullOrEmpty(commandNameField.text) ? "\n\n" + "; " + commandNameField.text : string.Empty + "\n" + copyBufferField.text;

		public void StringField(ICommandParameterData<string> data, params ToggleGroupData[] toggleGroup) 
		{
			InputField inputField = Instantiate(inputFieldPrototype, targetContainer);
			inputField.Initialize(data, toggleGroup);
		}

		public void StringDropdownField(ICommandParameterData<string> data, string[] stringValues, params ToggleGroupData[] toggleGroup) 
		{
			DropdownField inputField = Instantiate(dropdownFieldPrototype, targetContainer);
			inputField.Initialize(data, stringValues:stringValues, toggleGroup:toggleGroup);
		}

		public void TypeDropdownField<T>(ICommandParameterData<T> data, Dictionary<string, T> values, params ToggleGroupData[] toggleGroup) 
		{
			DropdownField dropdownField = Instantiate(dropdownFieldPrototype, targetContainer);
			dropdownField.Initialize(data, stringValues:values.Keys.ToArray(), typeValues:values.Values.ToArray(), toggleGroup:toggleGroup);
		}

		public void BoolField(ICommandParameterData<bool> data, params ToggleGroupData[] toggleGroup) 
		{
			ToggleField toggleField = Instantiate(toggleFieldPrototype, targetContainer);
			toggleField.Initialize(data, toggleGroup);
		}

		public void IntField(ICommandParameterData<int> data, int? min, int? max, params ToggleGroupData[] toggleGroup) 
		{
			ScrollableInputField inputField = Instantiate(scrollableFieldPrototype, targetContainer);
			inputField.Initialize(data, min:min, max:max, toggleGroup: toggleGroup);
		}

		public void FloatField(ICommandParameterData<float> data, float? min = null, float? max = null, params ToggleGroupData[] toggleGroup) 
		{
			ScrollableInputField inputField = Instantiate(scrollableFieldPrototype, targetContainer);
			inputField.Initialize(data, min:min, max:max, toggleGroup:toggleGroup);
		}

		public void FloatSliderField(ICommandParameterData<float> data, float min, float max, params ToggleGroupData[] toggleGroup)
		{
			SliderField sliderField = Instantiate(sliderFieldPrototype, targetContainer);
			sliderField.Initialize(data, min, max, toggleGroup);
		}

		public void IntSliderField(ICommandParameterData<int> data, int min, int max, params ToggleGroupData[] toggleGroup) 
		{
			SliderField sliderField = Instantiate(sliderFieldPrototype, targetContainer);
			sliderField.Initialize(data, min, max, toggleGroup);
		}

		public void ColorField(ICommandParameterData<Color> data, bool includeAlpha = true, bool includeHDR = false, params ToggleGroupData[] toggleGroup) 
		{
			ColorField colorField = Instantiate(colorFieldPrototype, targetContainer);
			colorField.Initialize(data, includeAlpha, includeHDR, toggleGroup);
		}

		public void EnumDropdownField(ICommandParameterData<Enum> data, params ToggleGroupData[] toggleGroup) 
		{
			DropdownField dropdownField = Instantiate(dropdownFieldPrototype, targetContainer);
			dropdownField.Initialize(data, toggleGroup:toggleGroup);
		}

		public void Vector2Field(ICommandParameterData<Vector2> data, params ToggleGroupData[] toggleGroup) 
		{
			ScrollableInputField inputField = Instantiate(scrollableFieldPrototype, targetContainer);
			inputField.Initialize(data, toggleGroup:toggleGroup);
		}

		public void Vector3Field(ICommandParameterData<Vector3> data, params ToggleGroupData[] toggleGroup) 
		{
			ScrollableInputField inputField = Instantiate(scrollableFieldPrototype, targetContainer);
			inputField.Initialize(data, toggleGroup:toggleGroup);
		}

		public void Vector4Field(ICommandParameterData<Vector4> data, params ToggleGroupData[] toggleGroup) 
		{
			ScrollableInputField inputField = Instantiate(scrollableFieldPrototype, targetContainer);
			inputField.Initialize(data, toggleGroup:toggleGroup);
		}

		public void PosField(ICommandParameterData<Vector3> data, CameraConfiguration cameraConfiguration, params ToggleGroupData[] toggleGroup) 
		{
			ScrollableInputField inputField = Instantiate(scrollableFieldPrototype, targetContainer);
			inputField.Initialize(data, cameraConfiguration:cameraConfiguration, isPos:true, toggleGroup:toggleGroup);
		}

		public void ListField(IListCommandParameterData list, params ToggleGroupData[] toggleGroup) 
		{
			ListField listField = Instantiate(listFieldPrototype, targetContainer);
			listField.Initialize(list, toggleGroup);
		}
	}
}