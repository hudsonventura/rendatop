import { useEffect } from "react";
import { useNavigate } from "react-router-dom";

const Logout = () => {
    const navigate = useNavigate();

    useEffect(() => {
        sessionStorage.clear();
        navigate("/Login", { replace: true });
    }, [navigate]);

    return null;
};

export default Logout;

