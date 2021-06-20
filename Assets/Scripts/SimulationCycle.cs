using System.Collections;
using System.Collections.Generic;
using System;
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

    [Range(1, 3)]
    public int numAgentTypes = 3;
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
    public int guideSize = 1;

    public bool randomBounce = true;

    public Config initialConfig = Config.RandomSpread;
    [Range(0, 1)]
    public float R = 1;
    [Range(0, 1)]
    public float G = 1;
    [Range(0, 1)]
    public float B = 1;

    [Range(0, 1)]
    public float R2 = 0;
    [Range(0, 1)]
    public float G2 = 1;
    [Range(0, 1)]
    public float B2 = 0;

    [Range(0, 1)]
    public float R3 = 0;
    [Range(0, 1)]
    public float G3 = 0;
    [Range(0, 1)]
    public float B3 = 1;

    public bool showTrail = true;
    public bool colorAgent = true;
    public bool highlightGuideAgent = false;
    public bool whiteAgentPixel = true;

    public bool simHasStarted = false;

    RenderTexture renderMap;
    RenderTexture agentMap;
    RenderTexture trailMap;

    ComputeBuffer agentBuffer;

    // setters. might be tied to "on value change" of slide bar
    public void SetTargetFrameRate(float frameRate)
    {
        targetFrameRate = (int) frameRate;
    }
    public void SetTrailLife(float trailLife)
    {
        this.trailLife = trailLife;
    }
    public void SetSpeed(float speed)
    {
        this.speed = speed;
    }
    public void SetSideAttractionWeight(float sideAttractionWeight)
    {
        this.sideAttractionWeight = sideAttractionWeight;
    }
    public void SetCenterAttractionWeight(float centerAttractionWeight)
    {
        this.centerAttractionWeight = centerAttractionWeight;
    }
    public void SetSensorDistance(float sensorDistance)
    {
        this.sensorDistance = sensorDistance;
    }
    public void SetSensorAngle(float sensorAngle)
    {
        this.sensorAngle = sensorAngle;
    }
    public void SetTurnStrenth(float turnStrenth)
    {
        this.turnStrenth = turnStrenth;
    }
    public void SetGuideSize(float guideSize)
    {
        this.guideSize = (int) (guideSize * 2 - 1);
    }
    public void SetR(float R)
    {
        this.R = R / 255;
    }
    public void SetG(float G)
    {
        this.G = G / 255;
    }
    public void SetB(float B)
    {
        this.B = B / 255;
    }
    public void SetR2(float R2)
    {
        this.R2 = R2 / 255;
    }
    public void SetG2(float G2)
    {
        this.G2 = G2 / 255;
    }
    public void SetB2(float B2)
    {
        this.B2 = B2 / 255;
    }
    public void SetR3(float R3)
    {
        this.R3 = R3 / 255;
    }
    public void SetG3(float G3)
    {
        this.G3 = G3 / 255;
    }
    public void SetB3(float B3)
    {
        this.B3 = B3 / 255;
    }
    public void SetShowTrail(bool showTrail)
    {
        this.showTrail = showTrail;
    }
    public void SetColorAgent(bool colorAgent)
    {
        this.colorAgent = colorAgent;
    }
    public void SetHighlightGuideAgent(bool highlightGuideAgent)
    {
        this.highlightGuideAgent = highlightGuideAgent;
    }
    public void SetRandomBounce(bool randomBounce)
    {
        this.randomBounce = randomBounce;
    }
    public void SetNumAgents(string numAgents)
    {
        this.numAgents = Convert.ToInt32(numAgents);
    }
    public void SetWidth(string width)
    {
        this.width = Convert.ToInt32(width);
    }
    public void SetHeight(string height)
    {
        this.height = Convert.ToInt32(height);
    }
    public void SetNumAgentTypes(float numAgentTypes)
    {
        int n = (int) numAgentTypes;
        if (n > 3) n = 3;
        this.numAgentTypes = n;
    }
    public void SetInitialConfig(int configIndex)
    {
        if (configIndex == 0) initialConfig = Config.FromCenter;
        if (configIndex == 1) initialConfig = Config.RandomSpread;
    }
    public void SetWhiteAgentPixel(bool whiteAgentPixel)
    {
        this.whiteAgentPixel = whiteAgentPixel;
    }

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
                    var direction = new Vector2(UnityEngine.Random.Range(-bigNum, bigNum), UnityEngine.Random.Range(-bigNum, bigNum));
                    direction.Normalize();

                    agents[i].position = new Vector2(width / 2, height / 2);
                    agents[i].direction = direction;
                }
            break;
            case Config.RandomSpread:
                for (var i = 0; i < agents.Length; i++)
                {
                    var direction = new Vector2(UnityEngine.Random.Range(-bigNum, bigNum), UnityEngine.Random.Range(-bigNum, bigNum));
                    direction.Normalize();

                    agents[i].position = new Vector2(UnityEngine.Random.Range(0, width - 1), UnityEngine.Random.Range(0, height - 1));
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

    float[] getColor(float R, float G, float B)
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
        flowShader.SetInt("agentTypeSize", (int) (numAgents / numAgentTypes));
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
        diffuseShader.SetInt("width", width);
        diffuseShader.SetInt("height", height);

        diffuseShader.SetTexture(KERNEL_NUM, "TrailMap", trailMap);

        diffuseShader.Dispatch(KERNEL_NUM, width / IMAGE_THREAD_GROUP_SIZE, height / IMAGE_THREAD_GROUP_SIZE, 1);
    }

    void dispatchDrawAgentShader()
    {
        drawAgentShader.SetInt("width", width);
        drawAgentShader.SetInt("height", height);
        drawAgentShader.SetBool("showTrail", showTrail);
        drawAgentShader.SetBool("colorAgent", colorAgent);
        drawAgentShader.SetBool("highlightGuideAgent", highlightGuideAgent);
        drawAgentShader.SetBool("whiteAgentPixel", whiteAgentPixel);
        drawAgentShader.SetFloats("color", getColor(R, G, B));
        drawAgentShader.SetFloats("color2", getColor(R2, G2, B2));
        drawAgentShader.SetFloats("color3", getColor(R3, G3, B3));

        drawAgentShader.SetTexture(KERNEL_NUM, "AgentMap", agentMap);
        drawAgentShader.SetTexture(KERNEL_NUM, "TrailMap", trailMap);
        drawAgentShader.SetTexture(KERNEL_NUM, "RenderMap", renderMap);
        drawAgentShader.SetBuffer(KERNEL_NUM, "agents", agentBuffer);

        drawAgentShader.Dispatch(KERNEL_NUM, width / IMAGE_THREAD_GROUP_SIZE, height / IMAGE_THREAD_GROUP_SIZE, 1);

        if (highlightGuideAgent)
        {
            drawAgentShader.SetInt("width", width);
            drawAgentShader.SetInt("height", height);

            drawAgentShader.SetBuffer(1, "agents", agentBuffer);
            drawAgentShader.SetTexture(1, "RenderMap", renderMap);

            drawAgentShader.Dispatch(1, 1, 1, 1);
        }
    }

    void dispatchShaders()
    {
        dispatchDecayShader();
        dispatchFlowShader();
        dispatchDiffuseShader();
        dispatchDrawAgentShader();
    }

    public void StartSimulation()
    {
        initializeMaps();
        initializeAgents();
        simHasStarted = true;
    }

    void Start()
    {
        Application.targetFrameRate = targetFrameRate;
        // StartSimulation();
    }

    private void Update()
    {
        if (!simHasStarted) return;
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
