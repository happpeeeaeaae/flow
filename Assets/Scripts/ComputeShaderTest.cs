using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Agent
{
    public Vector2 position;
    public float angle;
};

public class ComputeShaderTest : MonoBehaviour
{

    public ComputeShader computeShader;
    public RenderTexture renderTexture;

    Agent[] agents;
    ComputeBuffer agentBuffer;

    void Start()
    {
        int res = 1000;
        renderTexture = new RenderTexture(res, res, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        agents = new Agent[res];
        for (var i = 0; i < agents.Length; i++)
        {
            agents[i].position = new Vector2(res / 2, res / 2);
            agents[i].angle = Mathf.Sin(i * 71.01f) * 500461564;
        }
        agentBuffer = new ComputeBuffer(res, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Agent)));
        agentBuffer.SetData(agents);
    }


    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        computeShader.SetTexture(0, "TrailMap", renderTexture);
        computeShader.SetInt("width", renderTexture.width);
        computeShader.SetInt("height", renderTexture.height);
        computeShader.SetInt("numAgents", renderTexture.width);
        computeShader.SetFloat("moveSpeed", 5f);
        computeShader.SetFloat("deltaTime", Time.deltaTime);
        computeShader.SetBuffer(0, "agents", agentBuffer);
        computeShader.Dispatch(0, renderTexture.width / 16, renderTexture.height, 1);

        Graphics.Blit(renderTexture, destination);
    }

    private void OnDestroy()
    {
        agentBuffer.Dispose();
    }

}
