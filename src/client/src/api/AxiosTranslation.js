import axios from "axios";

export const getLanguages = async () => {
    try {
        const response = await axios.get('/api/translations/languages');
        return response.data;
    } catch (error) {
        console.error('Error fetching languages', error);
        return [];
    }
};

export const getTranslationsByNameSpace = async (namespace, language) => {
    try {
        const response = await axios.get(`/api/translations/${language}/${namespace}`);
        return response.data;
    } catch (error) {
        console.error('Error fetching translations', error);
        return {};
    }
}

export const getEntiereTranslationsByNameSpace = async (namespace, language) => {
    try {
        const response = await axios.get(`/api/translations/all/${language}/${namespace}`);
        return response.data;
    } catch (error) {
        console.error('Error fetching translations', error);
        return {};
    }
}
export const updateTranslation = async (id_translation, text) => {
    try {
        const response = await axios.put('/api/translations/update-translation', {
            id_translation,
            text
        });
        console.log('Update response:', response.data);
        return response.data;
    } catch (error) {
        console.error('Error updating translation', error);
        return null;
    }
};
export const insertTranslation = async (id_language, translation_namespace, translation_key, text) => {
    try {
        const response = await axios.post('/api/translations/insert-translation', {
            id_language,
            translation_namespace,
            translation_key,
            text
        });
        return response.data;
    } catch (error) {
        console.error('Error inserting translation', error);
        return null;
    }
};

export const getTranslationDashboard = async () => {
    try {
        const response = await axios.get('/api/translations/dashboard');
        return response.data;
    } catch (error) {
        console.error('Error fetching translation dashboard', error);
        return null;
    }
};

export const autoTranslate = async (sourceLangId, targetLangId, texts) => {
    try {
        const response = await axios.post('/api/translations/auto-translate', {
            sourceLangId,
            targetLangId,
            texts
        });
        console.log('Auto-translate response:', response.data);
        return response.data;
    } catch (error) {
        console.error('Error auto-translating', error);
        return null;
    }
};

export const getLanguagesId = async () => {
    try {
        const response = await axios.get(`${import.meta.env.VITE_API_BASE_URL ?? ''}/api/translations`);
        
        // 1. Axios stores the parsed JSON directly in the .data property.
        // 2. Axios automatically throws an error if the status is not 2xx.
        return response.data; 
        
    } catch (error) {
        // This catch block will trigger for 4xx/5xx responses or network errors.
        console.error('Error fetching languages by id:', error.message);
        throw error; // Re-throwing allows i18n.ts to handle the failure.
    }
};
