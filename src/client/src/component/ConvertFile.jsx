import React, { useState } from "react";
import { toast } from "sonner";
import Loader from "./Loader";
import { FaArrowLeft, FaUpload, FaDownload } from "react-icons/fa";
import { useHistory } from "react-router-dom";
import * as api from "../api/AxiosAvif";
import "../style/ConvertFile.css";

const ConvertFile = () => {
    const history = useHistory();

    const [file, setFile] = useState(null);
    const [originalFileName, setOriginalFileName] = useState("");
    const [isLoading, setIsLoading] = useState(false);
    const [convertedUrl, setConvertedUrl] = useState(null);

    const handleFileChange = (e) => {
        const selectedFile = e.target.files[0];
        if (!selectedFile) return;

        setFile(selectedFile);
        setOriginalFileName(selectedFile.name);
        setConvertedUrl(null);
    };

    const getAvifFileName = () => {
        if (!originalFileName) return "converted.avif";
        return originalFileName.replace(/\.[^/.]+$/, ".avif");
    };

    const handleConvert = async () => {
        if (!file) {
            toast.error("Il faut sélectionner une image");
            return;
        }

        setIsLoading(true);

        try {
            const response = await api.convertToAvif(file);
            const blob = new Blob([response.data], { type: "image/avif" });
            const url = URL.createObjectURL(blob);

            setConvertedUrl(url);
            toast.success("L'image a été convertie en AVIF avec succès !");
        } catch (error) {
            console.error(error);
            toast.error("Erreur lors de la conversion");
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="convert-file-page">
            <Loader show={isLoading} text="Conversion en cours..." />

            <div className="convert-file-card">
                <button
                    onClick={() => history.goBack()}
                    className="convert-file-back"
                >
                    <FaArrowLeft /> Retour
                </button>

                <h2 className="convert-file-title">
                    Convertir une image en AVIF
                </h2>

                <input
                    type="file"
                    accept="image/*"
                    onChange={handleFileChange}
                    className="convert-file-input"
                />

                <button
                    onClick={handleConvert}
                    className="convert-file-button"
                >
                    <FaUpload /> Convertir
                </button>

                {convertedUrl && (
                    <div className="convert-file-result">
                        <img
                            src={convertedUrl}
                            alt="AVIF preview"
                            className="convert-file-preview"
                        />
                        <a
                            href={convertedUrl}
                            download={getAvifFileName()}
                            className="convert-file-download"
                        >
                            <FaDownload /> Télécharger AVIF
                        </a>
                    </div>
                )}
            </div>
        </div>
    );
};

export default ConvertFile;
