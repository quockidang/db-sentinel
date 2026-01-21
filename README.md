# 🛡️ DB-Sentinel: AI-Powered Database Monitor

**DB-Sentinel** là một AI Agent tự động giám sát, phân tích hiệu năng và chẩn đoán lỗi Database dựa trên cơ chế suy luận (Reasoning) của **Gemini 3** và framework **Semantic Kernel**.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Powered by Gemini 3](https://img.shields.io/badge/AI-Gemini%203-blue)](https://deepmind.google/technologies/gemini/)
[![Built with .NET 9](https://img.shields.io/badge/Built%20with-.NET%209-512bd4)](https://dotnet.microsoft.com/)

---

## 🚀 Tính năng cốt lõi (Core Features)

* **🔍 Real-time Ingestion:** Theo dõi Slow Query Logs và các chỉ số hệ thống (CPU, RAM, Connections).
* **🧠 Deep Reasoning:** Sử dụng Gemini 3 để phân tích Execution Plan (`EXPLAIN`) và tìm ra nguyên nhân gốc rễ (Root Cause).
* **🛠️ Smart Suggestion:** Tự động đề xuất câu lệnh tối ưu (Index creation, Query rewriting).
* **🛡️ Safety First:** Hoạt động ở chế độ Read-only, đảm bảo an toàn tuyệt đối cho dữ liệu nghiệp vụ.

---

## 🏗️ Kiến trúc hệ thống (Architecture)

Dự án áp dụng mô hình **Agentic Workflow** (ReAct Pattern):

1.  **Sensor:** Thu thập dữ liệu từ MySQL/PostgreSQL.
2.  **Brain (Semantic Kernel + Gemini 3):** Xử lý ngôn ngữ tự nhiên và lập kế hoạch chẩn đoán.
3.  **Tools:** Các Native Functions thực thi lệnh chẩn đoán (Explain, Schema Check).

---

## 🛠️ Tech Stack

* **Language:** C# / .NET 9
* **AI Orchestration:** Microsoft Semantic Kernel
* **LLM:** Google Gemini 3 (Preview/Flash)
* **Database Support:** MySQL, PostgreSQL
* **Infrastructure:** Docker & Docker Compose

---

## 📋 Hướng dẫn cài đặt (Quick Start)

### 1. Yêu cầu hệ thống
* .NET 9 SDK
* Docker Desktop
* Gemini API Key (từ Google AI Studio)

### 2. Cấu hình môi trường
Tạo file `.env` tại thư mục gốc:
```env
GEMINI_API_KEY=your_api_key_here
DB_CONNECTION_STRING="Server=localhost;Database=your_db;Uid=root;Pwd=password;"
