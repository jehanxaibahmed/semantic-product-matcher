# 🔎 Semantic Product Matcher

![Status](https://img.shields.io/badge/status-in%20progress-orange?style=for-the-badge) ![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white) ![PostgreSQL](https://img.shields.io/badge/PostgreSQL-336791?style=for-the-badge&logo=postgresql&logoColor=white) ![pgvector](https://img.shields.io/badge/pgvector-336791?style=for-the-badge&logo=postgresql&logoColor=white) ![OpenAI](https://img.shields.io/badge/OpenAI-412991?style=for-the-badge&logo=openai&logoColor=white)

> Match messy, customer-written product names to the right catalogue item using embeddings and vector search.

## 🎯 Why this project

Customers rarely use exact product codes: they write "2 boxes of the large red peppers". This service embeds the catalogue, searches it with pgvector, and learns from confirmed matches so each customer's usual phrasing ranks higher next time.

## 🧱 Planned stack

- .NET 8 Web API
- PostgreSQL with the pgvector extension
- OpenAI embeddings with a local cache
- Match-history table for learning from feedback
- Docker Compose for local setup

## 🗺️ Roadmap

- [ ] Catalogue import and embedding job
- [ ] Vector search endpoint with top-k results
- [ ] Embedding cache to cut cost
- [ ] Customer match history and re-ranking
- [ ] Confidence thresholds for human review
- [ ] Benchmark on a sample catalogue

## 📌 Status

🚧 This project is in early development. Code is coming soon. It uses synthetic sample data only.

---

Built by [Jahanzaib Ahmad](https://github.com/jehanxaibahmed) · Full Stack Engineer · AI & LLM Systems
