import axios from "axios";

export const getLanguages = async () => {
    try {
        const response = await axios.get('/api/languages');
        return response.data;
    } catch (error) {
        console.error('Error fetching languages', error);
        return [];
    }
};

export const getLanguagesId = async () => {
    try {
        const response = await fetch('http://localhost:5078/api/translations');

        if (!response.ok) {
            throw new Error('Failed to load languages');
        }

        const data = await response.json();
        return data;
    } catch (error) {
        console.error('Error fetching languages by id', error);
    }
};