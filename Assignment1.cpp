#include <glad/glad.h>
#include <GLFW/glfw3.h>

#include "Shader.h"
#include <iostream>
#include <algorithm>

const unsigned int SCR_WIDTH = 800;
const unsigned int SCR_HEIGHT = 600;

static int gWidth = SCR_WIDTH;
static int gHeight = SCR_HEIGHT;

static double gMouseX = SCR_WIDTH * 0.5;
static double gMouseY = SCR_HEIGHT * 0.5;

static int gSkyMode = 2;
static bool gWDown = false;

void framebuffer_size_callback(GLFWwindow*, int width, int height)
{
    gWidth = (std::max)(1, width);
    gHeight = (std::max)(1, height);
    glViewport(0, 0, gWidth, gHeight);
}

void cursor_position_callback(GLFWwindow*, double xpos, double ypos)
{
    gMouseX = xpos;
    gMouseY = ypos;
}

void processInput(GLFWwindow* window)
{
    if (glfwGetKey(window, GLFW_KEY_ESCAPE) == GLFW_PRESS)
        glfwSetWindowShouldClose(window, true);

    // W toggles mode
    bool wNow = (glfwGetKey(window, GLFW_KEY_W) == GLFW_PRESS);
    if (wNow && !gWDown)
    {
        gSkyMode = (gSkyMode + 1) % 3; // night -> day -> purple
    }
    gWDown = wNow;
}

int main()
{
    glfwInit();
    glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3);
    glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 3);
    glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);
#ifdef __APPLE__
    glfwWindowHint(GLFW_OPENGL_FORWARD_COMPAT, GL_TRUE);
#endif

    GLFWwindow* window = glfwCreateWindow(SCR_WIDTH, SCR_HEIGHT,
        "Parallax City", NULL, NULL);
    if (!window)
    {
        std::cout << "Failed to create GLFW window\n";
        glfwTerminate();
        return -1;
    }
    glfwMakeContextCurrent(window);

    glfwSetFramebufferSizeCallback(window, framebuffer_size_callback);
    glfwSetCursorPosCallback(window, cursor_position_callback);

    if (!gladLoadGLLoader((GLADloadproc)glfwGetProcAddress))
    {
        std::cout << "Failed to initialize GLAD\n";
        return -1;
    }

    Shader shader("Assignment1.vs", "Assignment1.fs");

    // Fullscreen quad
    float vertices[] = {
        // positions          // tex coords
         1.0f,  1.0f, 0.0f,   1.0f, 1.0f,
         1.0f, -1.0f, 0.0f,   1.0f, 0.0f,
        -1.0f, -1.0f, 0.0f,   0.0f, 0.0f,
        -1.0f,  1.0f, 0.0f,   0.0f, 1.0f
    };
    unsigned int indices[] = { 0, 1, 3, 1, 2, 3 };

    unsigned int VAO, VBO, EBO;
    glGenVertexArrays(1, &VAO);
    glGenBuffers(1, &VBO);
    glGenBuffers(1, &EBO);

    glBindVertexArray(VAO);

    glBindBuffer(GL_ARRAY_BUFFER, VBO);
    glBufferData(GL_ARRAY_BUFFER, sizeof(vertices), vertices, GL_STATIC_DRAW);

    glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, EBO);
    glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(indices), indices, GL_STATIC_DRAW);

    glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 5 * sizeof(float), (void*)0);
    glEnableVertexAttribArray(0);

    glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, 5 * sizeof(float), (void*)(3 * sizeof(float)));
    glEnableVertexAttribArray(1);

    shader.use();
    int locTime = glGetUniformLocation(shader.ID, "u_time");
    int locRes = glGetUniformLocation(shader.ID, "u_resolution");
    int locMouse = glGetUniformLocation(shader.ID, "u_mouse");
    int locMode = glGetUniformLocation(shader.ID, "u_skyMode");

    while (!glfwWindowShouldClose(window))
    {
        processInput(window);

        glClearColor(0.02f, 0.01f, 0.03f, 1.0f);
        glClear(GL_COLOR_BUFFER_BIT);

        shader.use();
        glUniform1f(locTime, (float)glfwGetTime());
        glUniform2f(locRes, (float)gWidth, (float)gHeight);

        float mx = (float)(gMouseX / (double)(std::max)(1, gWidth));
        float my = (float)((gHeight - gMouseY) / (double)(std::max)(1, gHeight));
        glUniform2f(locMouse, mx, my);

        glUniform1i(locMode, gSkyMode);

        glBindVertexArray(VAO);
        glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0);

        glfwSwapBuffers(window);
        glfwPollEvents();
    }

    glDeleteVertexArrays(1, &VAO);
    glDeleteBuffers(1, &VBO);
    glDeleteBuffers(1, &EBO);

    glfwTerminate();
    return 0;
}
