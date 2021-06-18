using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Agent
{
    public Vector2 position;
    public Vector2 direction;
};

public enum Config
{
    FromCenter,
    RandomSpread
}

public class ComputeShaderTest : MonoBehaviour
{

    public ComputeShader computeShader;

    public int numAgents = 640000;
    public int width = 1920;
    public int height = 1080;

    [Range(0, 240)]
    public int targetFrameRate = 120;
    [Range(0, 0.1f)]
    public float dt = 0.005f;
    [Range(0 , 10)]
    public float decayRate = 1;

    [Range(0, 600)]
    public float speed = 600;

    [Range(0, 1)]
    public float sideAttractionThreshhold = 0.0f;
    [Range(0, 1)]
    public float sideAttractionWeight = 1f;
    [Range(0, 1)]
    public float centerAttractionThreshhold = 0.0f;
    [Range(0, 1)]
    public float centerAttractionWeight = 0.8f;
    [Range(0, 180)]
    public float sensorAngle = 20;
    [Range(0, 500)]
    public float sensorDistance = 25;
    [Range(0, 10000)]
    public float turnSpeed = 300;

    [Range(1, 30)]
    public int diffuseSize = 3;

    public bool randomBounce = true;

    public Config initialConfig = Config.RandomSpread;

    RenderTexture renderTexture;
    ComputeBuffer agentBuffer;

    int renderKernel;
    int threadGroupSizeX;
    int threadGroupSizeY;

    int flowKernel;
    int flowThreadGroupSize;

    Agent[] agents;

    void setUpFlowComputeShader()
    {
        renderKernel = computeShader.FindKernel("Render");
        uint x, y;
        computeShader.GetKernelThreadGroupSizes(renderKernel, out x, out y, out _);
        threadGroupSizeX = (int) x;
        threadGroupSizeY = (int) y;
    }

    void setUpAgentComputeShader()
    {
        flowKernel = computeShader.FindKernel("Flow");
        uint x;
        computeShader.GetKernelThreadGroupSizes(flowKernel, out x, out _, out _);
        flowThreadGroupSize = (int) x;
    }

    void initializeAgents()
    {
        agents = new Agent[numAgents];
        var bigNum = 1000000;

        switch (initialConfig)
        {
            case Config.FromCenter:
                for (var i = 0; i < agents.Length; i++)
                {
                    var direction = new Vector2(Random.Range(-bigNum, bigNum), Random.Range(-bigNum, bigNum));
                    direction.Normalize();

                    agents[i].position = new Vector2(width / 2, height / 2);
                    agents[i].direction = direction;
                }
            break;
            case Config.RandomSpread:
                for (var i = 0; i < agents.Length; i++)
                {
                    var direction = new Vector2(Random.Range(-bigNum, bigNum), Random.Range(-bigNum, bigNum));
                    direction.Normalize();

                    agents[i].position = new Vector2(Random.Range(0, width - 1), Random.Range(0, height - 1));
                    agents[i].direction = direction;
                }
                break;
        }


            agentBuffer = new ComputeBuffer(numAgents, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Agent)));
            agentBuffer.SetData(agents);
    }

    float smootheTurnAngle()
    {
        float angle = turnSpeed * Time.deltaTime;
        if (angle > 90) angle = 90;
        return angle * Mathf.Deg2Rad;
    }

    void setMutualShaderValues()
    {
        computeShader.SetBool("randomBounce", randomBounce);
        computeShader.SetInt("numAgents", numAgents);
        computeShader.SetInt("numAgents", numAgents);
        computeShader.SetInt("width", width);
        computeShader.SetInt("height", height);
        computeShader.SetInt("diffuseSize", diffuseSize);
        computeShader.SetFloat("speed", speed);
        computeShader.SetFloat("decayRate", decayRate);
        computeShader.SetFloat("sideAttractionThreshhold", sideAttractionThreshhold);
        computeShader.SetFloat("sideAttractionWeight", sideAttractionWeight);
        computeShader.SetFloat("centerAttractionThreshhold", centerAttractionThreshhold);
        computeShader.SetFloat("centerAttractionWeight", centerAttractionWeight);
        computeShader.SetFloat("turnSpeed", smootheTurnAngle());
        computeShader.SetFloats("sensorAngle", sensorAngle * Mathf.Deg2Rad);
        computeShader.SetFloats("sensorDistance", sensorDistance);
        computeShader.SetFloat("dt", dt);
    }

    void dispatchShaders()
    {
        setMutualShaderValues();

        computeShader.SetTexture(renderKernel, "TrailMap", renderTexture);
        computeShader.SetBuffer(renderKernel, "agents", agentBuffer);
        computeShader.Dispatch(renderKernel, width / threadGroupSizeX, height / threadGroupSizeY, 1);

        computeShader.SetTexture(flowKernel, "TrailMap", renderTexture);
        computeShader.SetBuffer(flowKernel, "agents", agentBuffer);
        computeShader.Dispatch(flowKernel, numAgents / flowThreadGroupSize, 1, 1);
        agentBuffer.GetData(agents);

    }

    void Start()
    {

        renderTexture = new RenderTexture(width, height, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        initializeAgents();
        setUpFlowComputeShader();
        setUpAgentComputeShader();
    }

    private void Update()
    {
        Application.targetFrameRate = targetFrameRate;
        dispatchShaders();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(renderTexture, destination);
    }

    private void OnDestroy()
    {
        agentBuffer.Dispose();
    }

}
