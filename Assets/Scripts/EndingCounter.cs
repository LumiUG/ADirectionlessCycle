using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Animator))]
public class EndingAnimation : MonoBehaviour
{
	public void EndScene() => UI.I.ChangeScene("Credits");

	public void RevealTextContent(string animationInput)
	{
		if (!animationInput.Contains(";")) return;
		string[] inputSplit = animationInput.Split(";");

		GameObject targetTextComponent = GameObject.Find(inputSplit[0]);
		if (!targetTextComponent) return;

		Text edit = targetTextComponent.GetComponent<Text>();
		edit.text = "";

		StartCoroutine(ShowTextOverTime(edit, inputSplit[1], inputSplit.Length == 2 ? 0.2f : float.Parse(inputSplit[2])));
	}

	private IEnumerator ShowTextOverTime(Text text, string content, float delay)
	{
		int visibleIndex = 0;
		
		do
		{
			text.text += content[visibleIndex];
			visibleIndex++;
			yield return new WaitForSecondsRealtime(delay);
		} while (text.text != content);
	}
}