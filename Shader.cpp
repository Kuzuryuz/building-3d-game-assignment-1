#include "Shader.h"
#include <fstream>
#include <sstream>
#include <iostream>

std::string Shader::readFile(const char* path) {
    std::ifstream file(path, std::ios::in);
    if (!file) {
        std::cerr << "Failed to open file: " << path << "\n";
        return {};
    }
    std::stringstream ss;
    ss << file.rdbuf();
    return ss.str();
}

unsigned int Shader::compile(GLenum type, const char* src) {
    unsigned int s = glCreateShader(type);
    glShaderSource(s, 1, &src, nullptr);
    glCompileShader(s);

    int ok = 0;
    glGetShaderiv(s, GL_COMPILE_STATUS, &ok);
    if (!ok) {
        char log[2048];
        glGetShaderInfoLog(s, 2048, nullptr, log);
        std::cerr << "Shader compile error:\n" << log << "\n";
    }
    return s;
}

Shader::Shader(const char* vertexPath, const char* fragmentPath) {
    std::string v = readFile(vertexPath);
    std::string f = readFile(fragmentPath);
    if (v.empty() || f.empty()) return;

    unsigned int vs = compile(GL_VERTEX_SHADER, v.c_str());
    unsigned int fs = compile(GL_FRAGMENT_SHADER, f.c_str());

    ID = glCreateProgram();
    glAttachShader(ID, vs);
    glAttachShader(ID, fs);
    glLinkProgram(ID);

    int ok = 0;
    glGetProgramiv(ID, GL_LINK_STATUS, &ok);
    if (!ok) {
        char log[2048];
        glGetProgramInfoLog(ID, 2048, nullptr, log);
        std::cerr << "Program link error:\n" << log << "\n";
    }

    glDeleteShader(vs);
    glDeleteShader(fs);
}

Shader::~Shader() {
    if (ID) glDeleteProgram(ID);
}

void Shader::use() const {
    glUseProgram(ID);
}
