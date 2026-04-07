import axios from "axios";

const API_URL = "/api";

export const convertToAvif = (file) => {
    const formData = new FormData();
    formData.append("file", file);

    return axios.post(
        `${API_URL}/file-converter`,
        formData,
        {
            responseType: "blob",
            headers: {
                "Content-Type": "multipart/form-data"
            }
        }
    );
};
