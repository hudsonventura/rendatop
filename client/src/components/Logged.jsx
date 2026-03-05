import React from 'react';
import { useEffect } from 'react';
import axiosInstance from '../utils/axiosConfig';

/**
 * A pagina que incorporar este componente, necessitará estar logado para seer visitada. 
 * Caso contrato este componente redicionará para a tela de login
 */
const Logged = () => {

    useEffect(() => {
        const token = sessionStorage.getItem('token');
        if (!token) {
            return;
        }
    }, []);

    useEffect(() => {
        axiosInstance
          .get("/Authenticated") // `/posts` será concatenado ao `baseURL`
          .then((response) => {

          })
          .catch((error) => {
            if (error.response?.status === 401) {
                sessionStorage.clear();
                window.location.href = '/Login';
              }
          });
      }, []);


    return (
        <>
            
        </>
    );
};

export default Logged;

