using ArchieML;
using Newtonsoft.Json.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class UnityArchieMLTest : MonoBehaviour {

    [Header("Edit ArchieML and see resulting JSON below.")]

    [TextArea(25, 100)]
    public string ArchieInput = @"
{config}
language: c#
version: 1.0
{}
            
copy.lorem: ipsum

[people]
* alice
* bob
* charlie
    ";

    [TextArea(25,100)]
    public string JsonOutput;

    public void Update() {
        JsonOutput = Archie.Load(ArchieInput).ToString();
    }
}