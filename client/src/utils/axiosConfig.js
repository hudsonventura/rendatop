import axios from "axios";

// Axios instance — cookies (HttpOnly jwt) are sent automatically via withCredentials
const axiosInstance = axios.create({
	baseURL: import.meta.env.VITE_API_URL,
	timeout: 60000,
	withCredentials: true, // required for HttpOnly cookie to be sent cross-origin
	headers: {
		"Content-Type": "application/json",
	},
});

// Interceptador para requisições
axiosInstance.interceptors.request.use(
	(config) => {
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
