﻿#pragma once

#include "Direct3DBase.h"
#include "SpriteBatch.h"
#include "SpriteFont.h"

struct ModelViewProjectionConstantBuffer
{
	DirectX::XMFLOAT4X4 model;
	DirectX::XMFLOAT4X4 view;
	DirectX::XMFLOAT4X4 projection;
};

struct VertexPositionColor
{
	DirectX::XMFLOAT3 pos;
	DirectX::XMFLOAT3 color;
};

// このクラスは、スピンしている立方体を描画します。
ref class CubeRenderer sealed : public Direct3DBase
{
public:
	CubeRenderer();

	// Direct3DBase メソッド。
	virtual void CreateDeviceResources() override;
	virtual void CreateWindowSizeDependentResources() override;
	virtual void Render() override;
	
	// 時間に依存するオブジェクトを更新するメソッドです。
	void Update(float timeTotal, float timeDelta);


	void setScreenData(const Platform::Array<byte>^ data);

	int GetDebugValue();

private:
	bool m_loadingComplete;
	long hResult;

	ID3D11ShaderResourceView* texture;
	Microsoft::WRL::ComPtr<ID3D11ShaderResourceView> m_texture1;
	std::unique_ptr<DirectX::SpriteBatch> spriteBatch;
	std::unique_ptr<DirectX::SpriteFont> m_font;
	
	static const int SCREEN_BUF_MAX = 1000000; // 1MB
	// Platform::Array<byte>^ screen_buf;
	uint8_t screen_buf[SCREEN_BUF_MAX];
	int screen_buf_size;

	Microsoft::WRL::ComPtr<ID3D11InputLayout> m_inputLayout;
	Microsoft::WRL::ComPtr<ID3D11Buffer> m_vertexBuffer;
	Microsoft::WRL::ComPtr<ID3D11Buffer> m_indexBuffer;
	Microsoft::WRL::ComPtr<ID3D11VertexShader> m_vertexShader;
	Microsoft::WRL::ComPtr<ID3D11PixelShader> m_pixelShader;
	Microsoft::WRL::ComPtr<ID3D11Buffer> m_constantBuffer;

	uint32 m_indexCount;
	ModelViewProjectionConstantBuffer m_constantBufferData;
};