#pragma once
#include <string>
#include <glad/glad.h>

class Shader {
public:
    unsigned int ID = 0;

    Shader(const char* vertexPath, const char* fragmentPath);
    ~Shader();

    void use() const;

private:
    static std::string readFile(const char* path);
    static unsigned int compile(GLenum type, const char* src);
};
