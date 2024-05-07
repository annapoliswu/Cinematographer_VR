using UnityEngine;
using UnityEngine.Events;
using Python.Runtime;
using TMPro;
using System.Text.RegularExpressions;



public class PythonInterfacer : MonoBehaviour
{
    

    dynamic np, pickle, model;
    public TextMeshPro tmp;

    public enum ModelType { Expert, Researcher };
    public UnityEvent onModelChange;
    [SerializeField]
    private ModelType modelType;
    public int historySize = 100;
    public float forcastThreshold = .425f;

    public class ModelInput
    {
        public object model;
        public PyList features2D; //double array
        public float forcastThreshold;

        public ModelInput(object m , PyList f2D, float fT)
        {
            model = m;
            features2D = f2D;
            this.forcastThreshold = fT;
        }

        public ModelInput(object m, float[] f, float fT)
        {
            model = m;
            this.forcastThreshold = fT;

            PyList features1D = new PyList();
            for (int i = 0; i < f.Length; i++)
            {
                features1D.Append(new PyFloat(f[i]));
            }
            features2D = new PyList();
            features2D.Append(features1D);
        }
    }

    public void ChangeModel(ModelType newModelType)
    {
        if (modelType != newModelType)
        {
            modelType = newModelType;
            string filePath;
            if (modelType == ModelType.Expert)
            {
                filePath = Application.dataPath + "/StreamingAssets/models/expert/model_chunk" + historySize + ".pkl";
            }
            else 
            {
                filePath = Application.dataPath + "/StreamingAssets/models/researcher/model_chunk" + historySize + ".pkl";
            }
            PythonEngine.Initialize();
            model = RunPythonCodeAndReturn(
                @"
import os
from sklearn.ensemble import RandomForestClassifier
import pickle

ret = os.listdir()
file = open(filePath, 'rb'); 
model = pickle.load(file);
ret = model
",
                filePath,
                "filePath",
                "ret");

            onModelChange?.Invoke();
        }
    }

    // Import model from files once
    void Start()
    {

        tmp = this.GetComponent<TextMeshPro>();
        Runtime.PythonDLL = Application.dataPath + "/StreamingAssets/embedded-python/python311.dll";
        Debug.Log("Python interfacer started");

        if (onModelChange == null)
            onModelChange = new UnityEvent();
        ChangeModel(ModelType.Researcher);

        }



    public int ModelPrediction(PyList featureArray)
    {
        ModelInput modelInput = new ModelInput(model, featureArray, forcastThreshold);
        return ModelPrediction(modelInput);
    }
    public int ModelPrediction(float [] featureArray)
    {
        ModelInput modelInput = new ModelInput(model, featureArray, forcastThreshold);
        return ModelPrediction(modelInput);
    }


    public int ModelPrediction(ModelInput modelInput)
    {
        object ret = RunPythonCodeAndReturn(
      @"
import numpy as np
from sklearn.ensemble import RandomForestClassifier
from sklearn.svm import SVC, LinearSVC
import pickle


clf = modelInput.model
X = modelInput.features2D

probaArray = clf.predict_proba(X)[0]
maxProbaArg = probaArray.argmax()
maxProba = probaArray[maxProbaArg]

if maxProba > modelInput.forcastThreshold:
    ret = clf.classes_[maxProbaArg]
else:
    ret = -1
    
",
      modelInput,
      "modelInput",
      "ret");

        string retString = ret.ToString();
        //print(retString);
        return int.Parse(Regex.Match(retString, @"-?\d+").Value);

    }

    public void OnApplicationQuit()
    {
        if (PythonEngine.IsInitialized)
        {
            print("Ending python");
            PythonEngine.Shutdown();
        }
    }

    //helper function to run python code
    public static object RunPythonCodeAndReturn(string pycode, object parameter, string parameterName, string returnedVariableName)
    {
        object returnedVariable = new object();
        using (Py.GIL())
        {
            using (var scope = Py.CreateScope())
            {
                scope.Set(parameterName, parameter.ToPython());
                scope.Exec(pycode);
                returnedVariable = scope.Get<object>(returnedVariableName);
            }
        }
        return returnedVariable;
    }
}
