import axios from "axios";
import { useNavigate } from "react-router-dom";

// Cria uma instância do Axios com configurações padrão
const axiosInstance = axios.create({
	baseURL: import.meta.env.VITE_API_URL, // URL base para todas as requisições
	timeout: 30000, // 30 seconds — gives the server time during cold start
	headers: {
		"Content-Type": "application/json",
	},
});

// Token is read dynamically on every request (see interceptor below)

// Interceptador para requisições
axiosInstance.interceptors.request.use(
	(config) => {
		// Always read the latest token so post-login requests are authenticated
		const token = sessionStorage.getItem("token");
		if (token) {
			config.headers["Authorization"] = `Bearer ${token}`;
		}

		var env = import.meta.env.VITE_ENVIRONMENT;
		if (env == "Development")
			console.log("Requisição enviada:", config);

		return config;
	},
	(error) => {
		var env = import.meta.env.VITE_ENVIRONMENT;
		if (env == "Development")
			console.error("Erro na requisição:", error);

		return Promise.reject(error);
	}
);

// Interceptador para respostas
axiosInstance.interceptors.response.use(
	(response) => {
		var env = import.meta.env.VITE_ENVIRONMENT;
		if (env == "Development")
			console.log("Resposta recebida:", response);

		return response;
	},
	(error) => {
		var env = import.meta.env.VITE_ENVIRONMENT;
		if (env == "Development")
			console.error("Erro na resposta:", error);

		return Promise.reject(error);
	}
);

export default axiosInstance;

