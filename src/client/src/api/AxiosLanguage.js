import axios from "axios";

// CRUD Functions for Languages
export const createLanguage = async (name_language, code) => {
    try {
        const response = await axios.post('/api/translations/create-language', {
            name_language,
            code
        });
        return response.data;
    } catch (error) {
        console.error('Error creating language:', error);
        throw error;
    }
};

export const updateLanguage = async (id_language, name_language, code) => {
    try {
        const response = await axios.put('/api/translations/update-language', {
            id_language,
            name_language,
            code
        });
        return response.data;
    } catch (error) {
        console.error('Error updating language:', error);
        throw error;
    }
};

export const deleteLanguage = async (id_language) => {
    try {
        const response = await axios.delete(`/api/translations/delete-language/${id_language}`);
        return response.data;
    } catch (error) {
        console.error('Error deleting language:', error);
        throw error;
    }
};

