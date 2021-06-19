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

public class SimulationCycle : MonoBehaviour
{
    const int STRENTH = 1000;
    const float DELTA_TIME = 0.01f;
    const int AGENT_THREAD_GROUP_SIZE = 64; // (64,1,1)
    const int IMAGE_THREAD_GROUP_SIZE = 8; // (8,8,8)
    const int KERNEL_NUM = 0;

    public ComputeShader flowShader;
    public ComputeShader diffuseShader;
    public ComputeShader decayShader;
    public ComputeShader drawAgentShader;

    public int numAgents = 1000000;
    public int width = 1920;
    public int height = 1080;

    [Range(0, 240)]
    public int targetFrameRate = 240;
    [Range(0 , 6)]
    public float trailLife = 1;

    [Range(0, 600)]
    public float speed = 100;

    [Range(0, 1)]
    public float sideAttractionWeight = 1f;
    [Range(0, 1)]
    public float centerAttractionWeight = 1f;
    [Range(0, 500)]
    public float sensorDistance = 50;
    [Range(0, 180)]
    public float sensorAngle = 10;
    [Range(-STRENTH / 2, STRENTH / 2)]
    public float turnStrenth = 100;

    [Range(1, 30)]
    public int diffuseSize = 1;

    public bool randomBounce = true;

    public Config initialConfig = Config.RandomSpread;
    [Range(0, 1)]
    public float R = 1;
    [Range(0, 1)]
    public float G = 1;
    [Range(0, 1)]
    public float B = 1;
    public bool showTrail = true;
    public bool colorAgent = true;
    public bool highlightGuideAgent = false;

    RenderTexture renderMap;
    RenderTexture agentMap;
    RenderTexture trailMap;

    ComputeBuffer agentBuffer;

    Agent[] agents;

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

    void initializeMaps()
    {
        renderMap = new RenderTexture(width, height, 24);
        renderMap.enableRandomWrite = true;
        renderMap.Create();

        agentMap = new RenderTexture(width, height, 24);
        agentMap.enableRandomWrite = true;
        agentMap.Create();

        trailMap = new RenderTexture(width, height, 24);
        trailMap.enableRandomWrite = true;
        trailMap.Create();
    }

    float[] getColor()
    {
        float[] color = { R, G, B, 1};
        return color;
    }

    float turnCorrection()
    {
        return (turnStrenth / STRENTH / 2);
    }

    void dispatchFlowShader()
    {
        flowShader.SetBool("randomBounce", randomBounce);
        flowShader.SetInt("width", width);
        flowShader.SetInt("height", height);
        flowShader.SetFloat("speed", speed);
        flowShader.SetFloat("sideAttractionWeight", sideAttractionWeight);
        flowShader.SetFloat("centerAttractionWeight", centerAttractionWeight);
        flowShader.SetFloat("turnAngleSin", Mathf.Sin(sensorAngle * Mathf.Deg2Rad * turnCorrection()));
        flowShader.SetFloat("turnAngleCos", Mathf.Cos(sensorAngle * Mathf.Deg2Rad * turnCorrection()));
        flowShader.SetFloat("sensorDistance", sensorDistance);
        flowShader.SetFloat("dt", DELTA_TIME);
        flowShader.SetFloat("sensorAngleSin", Mathf.Sin(sensorAngle * Mathf.Deg2Rad));
        flowShader.SetFloat("sensorAngleCos", Mathf.Cos(sensorAngle * Mathf.Deg2Rad));

        flowShader.SetTexture(KERNEL_NUM, "TrailMap", trailMap);
        flowShader.SetTexture(KERNEL_NUM, "AgentMap", agentMap);
        flowShader.SetBuffer(KERNEL_NUM, "agents", agentBuffer);

        flowShader.Dispatch(KERNEL_NUM, numAgents / AGENT_THREAD_GROUP_SIZE, 1, 1);
        agentBuffer.GetData(agents);
    }

    void dispatchDecayShader()
    {
        decayShader.SetFloat("dt", DELTA_TIME);
        decayShader.SetFloat("trailLife", trailLife);

        decayShader.SetTexture(KERNEL_NUM, "TrailMap", trailMap);
        decayShader.Dispatch(KERNEL_NUM, width / IMAGE_THREAD_GROUP_SIZE, height / IMAGE_THREAD_GROUP_SIZE, 1);
    }

    void dispatchDiffuseShader()
    {
        diffuseShader.SetInt("diffuseSize", diffuseSize);
        diffuseShader.SetInt("width", width);
        diffuseShader.SetInt("height", height);

        diffuseShader.SetTexture(KERNEL_NUM, "TrailMap", trailMap);

        diffuseShader.Dispatch(KERNEL_NUM, width / IMAGE_THREAD_GROUP_SIZE, height / IMAGE_THREAD_GROUP_SIZE, 1);
    }

    void dispatchDrawAgentShader()
    {
        drawAgentShader.SetInt("diffuseSize", diffuseSize);
        drawAgentShader.SetInt("width", width);
        drawAgentShader.SetInt("height", height);
        drawAgentShader.SetBool("showTrail", showTrail);
        drawAgentShader.SetBool("colorAgent", colorAgent);
        drawAgentShader.SetBool("highlightGuideAgent", highlightGuideAgent);
        drawAgentShader.SetFloats("color", getColor());

        drawAgentShader.SetTexture(KERNEL_NUM, "AgentMap", agentMap);
        drawAgentShader.SetTexture(KERNEL_NUM, "TrailMap", trailMap);
        drawAgentShader.SetTexture(KERNEL_NUM, "RenderMap", renderMap);
        drawAgentShader.SetBuffer(KERNEL_NUM, "agents", agentBuffer);

        drawAgentShader.Dispatch(KERNEL_NUM, width / IMAGE_THREAD_GROUP_SIZE, height / IMAGE_THREAD_GROUP_SIZE, 1);
    }

    void dispatchShaders()
    {
        dispatchDecayShader();
        dispatchFlowShader();
        dispatchDiffuseShader();
        dispatchDrawAgentShader();
    }

    void Start()
    {
        initializeMaps();
        initializeAgents();
    }

    private void Update()
    {
        Application.targetFrameRate = targetFrameRate;
        dispatchShaders();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(renderMap, destination);
    }

    private void OnDestroy()
    {
        agentBuffer.Dispose();
    }

}
